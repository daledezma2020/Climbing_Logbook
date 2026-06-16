using api.DTO;
using api.Interfaces;
using api.Models;
using System.Text;
using System.Text.Json;

namespace api.Services;

public class OpenBetaClient : IOpenBetaClient
{
    private const string DefaultTypesenseHost = "https://typesense-01.openbeta.io";
    private const string DefaultTypesenseApiKey = "bCGIB4sHsJRy06NFjZLtxKIMgr4aO0tX";
    private const string DefaultGraphQlUrl = "https://api.openbeta.io";

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public OpenBetaClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<List<SearchResultDto>> SearchAsync(string query, int limit, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var baseUrl = _configuration["OpenBeta:TypesenseUrl"] ?? DefaultTypesenseHost;
        var apiKey = _configuration["OpenBeta:TypesenseApiKey"] ?? DefaultTypesenseApiKey;
        var url = $"{baseUrl.TrimEnd('/')}/multi_search";

        var payload = new
        {
            searches = new[]
            {
                new
                {
                    q = query,
                    query_by = "climbName,areaNames",
                    collection = "climbs",
                    exclude_fields = "climbDesc",
                    page = 1,
                    per_page = limit
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("X-TYPESENSE-API-KEY", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var hits = doc.RootElement
            .GetProperty("results")[0]
            .GetProperty("hits");

        var results = new List<SearchResultDto>();
        foreach (var hit in hits.EnumerateArray())
        {
            var document = hit.GetProperty("document");
            var disciplines = ReadStringArray(document, "disciplines");
            if (!disciplines.Contains("bouldering", StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var grade = ReadString(document, "grade");
            if (string.IsNullOrWhiteSpace(grade) || !grade.StartsWith("V", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var areaNames = ReadStringArray(document, "areaNames");
            var uuid = ReadString(document, "climbUUID");
            if (string.IsNullOrWhiteSpace(uuid))
            {
                continue;
            }

            results.Add(new SearchResultDto
            {
                Key = $"openbeta:climb:{uuid}",
                ResultType = "climb",
                Name = ReadString(document, "climbName") ?? "Unknown climb",
                Sources = ["openbeta"],
                Grade = new GradeDto("vscale", grade),
                Discipline = ClimbDiscipline.Bouldering,
                PlaceName = areaNames.LastOrDefault(),
                PlaceKind = PlaceKind.Outdoor,
                ExternalId = uuid
            });
        }

        return results;
    }

    public async Task<OpenBetaClimbDetails?> GetClimbAsync(string uuid, CancellationToken cancellationToken)
    {
        var graphQlUrl = _configuration["OpenBeta:GraphQlUrl"] ?? DefaultGraphQlUrl;
        var payload = new
        {
            query = """
                query Climb($uuid: ID) {
                  climb(uuid: $uuid) {
                    uuid
                    name
                    fa
                    grades { vscale }
                    type { bouldering }
                    metadata { lat lng }
                    parent { uuid areaName metadata { lat lng } }
                  }
                }
                """,
            variables = new { uuid }
        };

        using var response = await _httpClient.PostAsJsonAsync(graphQlUrl, payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var climb = doc.RootElement.GetProperty("data").GetProperty("climb");
        if (climb.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        var bouldering = climb.GetProperty("type").TryGetProperty("bouldering", out var boulderingElement)
            && boulderingElement.ValueKind != JsonValueKind.Null;
        if (!bouldering)
        {
            return null;
        }

        var grade = climb.GetProperty("grades").TryGetProperty("vscale", out var gradeElement)
            ? gradeElement.GetString()
            : null;
        if (string.IsNullOrWhiteSpace(grade))
        {
            return null;
        }

        var parent = climb.GetProperty("parent");
        var metadata = climb.GetProperty("metadata");
        var parentMetadata = parent.GetProperty("metadata");

        return new OpenBetaClimbDetails
        {
            Uuid = climb.GetProperty("uuid").GetString() ?? uuid,
            Name = climb.GetProperty("name").GetString() ?? "Unknown climb",
            Grade = grade,
            FirstAscentName = climb.TryGetProperty("fa", out var fa) ? fa.GetString() : null,
            ParentAreaUuid = parent.GetProperty("uuid").GetString(),
            ParentAreaName = parent.GetProperty("areaName").GetString(),
            Latitude = ReadDouble(metadata, "lat") ?? ReadDouble(parentMetadata, "lat"),
            Longitude = ReadDouble(metadata, "lng") ?? ReadDouble(parentMetadata, "lng")
        };
    }

    private static string? ReadString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string[] ReadStringArray(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).ToArray()
            : [];
    }

    private static double? ReadDouble(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetDouble()
            : null;
    }
}
