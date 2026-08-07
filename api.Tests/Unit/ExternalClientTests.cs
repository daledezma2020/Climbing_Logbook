using System.Net;
using System.Text;
using api.Models;
using api.Services;
using Microsoft.Extensions.Configuration;

namespace api.Tests.Unit;

public class ExternalClientTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task OpenBetaSearchReturnsOnlyImportableBoulders()
    {
        const string response = """
            {
              "results": [{
                "hits": [
                  { "document": { "climbUUID": "valid", "climbName": "Test Boulder", "disciplines": ["bouldering"], "grade": "V4", "areaNames": ["State", "Test Crag"] } },
                  { "document": { "climbUUID": "sport", "climbName": "Sport Route", "disciplines": ["sport"], "grade": "5.10", "areaNames": ["Crag"] } },
                  { "document": { "climbUUID": "font", "climbName": "Font Boulder", "disciplines": ["bouldering"], "grade": "6A", "areaNames": ["Crag"] } }
                ]
              }]
            }
            """;
        var handler = new RecordingHandler(response);
        var client = new OpenBetaClient(new HttpClient(handler), Configuration(
            ("OpenBeta:TypesenseUrl", "https://search.test"),
            ("OpenBeta:TypesenseApiKey", "test-key")));

        var results = await client.SearchAsync("test climb", 7, CancellationToken.None);

        var result = Assert.Single(results);
        Assert.Equal("openbeta:climb:valid", result.Key);
        Assert.Equal("Test Boulder", result.Name);
        Assert.Equal("V4", result.Grade?.Value);
        Assert.Equal("Test Crag", result.PlaceName);
        Assert.Equal(PlaceKind.Outdoor, result.PlaceKind);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("https://search.test/multi_search", handler.RequestUri?.ToString());
        Assert.Equal("test-key", handler.ApiKey);
        Assert.Contains("\"per_page\":7", handler.Body);
        Assert.Contains("\"q\":\"test climb\"", handler.Body);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task OpenBetaSearchDoesNotCallTheNetworkForABlankQuery()
    {
        var handler = new RecordingHandler("{}");
        var client = new OpenBetaClient(new HttpClient(handler), Configuration());

        var results = await client.SearchAsync("  ", 10, CancellationToken.None);

        Assert.Empty(results);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task OpenBetaClimbUsesParentCoordinatesWhenClimbCoordinatesAreMissing()
    {
        const string response = """
            { "data": { "climb": {
              "uuid": "abc", "name": "Parent Pin", "fa": "Taylor",
              "grades": { "vscale": "V6" }, "type": { "bouldering": true },
              "metadata": { "lat": null, "lng": null },
              "parent": { "uuid": "area-1", "areaName": "Parent Area", "metadata": { "lat": 39.5, "lng": -76.25 } }
            } } }
            """;
        var handler = new RecordingHandler(response);
        var client = new OpenBetaClient(new HttpClient(handler), Configuration(("OpenBeta:GraphQlUrl", "https://graphql.test")));

        var result = await client.GetClimbAsync("abc", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Parent Pin", result.Name);
        Assert.Equal("V6", result.Grade);
        Assert.Equal(39.5, result.Latitude);
        Assert.Equal(-76.25, result.Longitude);
        Assert.Equal("https://graphql.test/", handler.RequestUri?.ToString());
        Assert.Contains("\"uuid\":\"abc\"", handler.Body);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task OsmPlaceUsesCenterCoordinatesAndAddressTags()
    {
        const string response = """
            { "elements": [{
              "type": "way", "id": 99,
              "center": { "lat": 40.1, "lon": -75.2 },
              "tags": { "name": "Test Gym", "addr:street": "1 Main St", "addr:city": "Reading", "addr:state": "PA", "addr:country": "US" }
            }] }
            """;
        var handler = new RecordingHandler(response);
        var client = new OsmClient(new HttpClient(handler), Configuration(("Osm:OverpassUrl", "https://overpass.test")));

        var result = await client.GetPlaceAsync("Way", "99", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Test Gym", result.Name);
        Assert.Equal(40.1, result.Latitude);
        Assert.Equal(-75.2, result.Longitude);
        Assert.Equal("1 Main St", result.Address);
        Assert.Equal("Reading", result.City);
        Assert.Contains("way%2899%29", handler.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("{ \"elements\": [] }")]
    [InlineData("{ \"elements\": [{ \"lat\": 1, \"lon\": 2, \"tags\": {} }] }")]
    [Trait("Category", "Unit")]
    public async Task OsmPlaceReturnsNullWhenNoNamedElementExists(string response)
    {
        var client = new OsmClient(new HttpClient(new RecordingHandler(response)), Configuration());

        Assert.Null(await client.GetPlaceAsync("node", "1", CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExternalClientsSurfaceUnsuccessfulHttpResponses()
    {
        var openBeta = new OpenBetaClient(
            new HttpClient(new RecordingHandler("{}", HttpStatusCode.BadGateway)),
            Configuration());
        var osm = new OsmClient(
            new HttpClient(new RecordingHandler("{}", HttpStatusCode.ServiceUnavailable)),
            Configuration());

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            openBeta.SearchAsync("test", 10, CancellationToken.None));
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            osm.GetPlaceAsync("node", "1", CancellationToken.None));
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(x => x.Key, x => (string?)x.Value))
            .Build();
    }

    private sealed class RecordingHandler(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        public HttpMethod? Method { get; private set; }
        public Uri? RequestUri { get; private set; }
        public string? ApiKey { get; private set; }
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            Method = request.Method;
            RequestUri = request.RequestUri;
            ApiKey = request.Headers.TryGetValues("X-TYPESENSE-API-KEY", out var values) ? values.Single() : null;
            Body = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
