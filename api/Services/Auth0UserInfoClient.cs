using api.Interfaces;
using System.Net.Http.Headers;
using System.Text.Json;

namespace api.Services;

public class Auth0UserInfoClient : IAuth0UserInfoClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<Auth0UserInfoClient> _logger;

    public Auth0UserInfoClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<Auth0UserInfoClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Auth0UserInfo?> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        var domain = _configuration["Auth0:Domain"];
        if (string.IsNullOrWhiteSpace(domain))
        {
            _logger.LogWarning("Auth0:Domain is not configured; skipping userinfo lookup.");
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://{domain}/userinfo");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Auth0 userinfo returned {StatusCode}.", (int)response.StatusCode);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            return new Auth0UserInfo
            {
                Name = ReadString(doc.RootElement, "name"),
                Nickname = ReadString(doc.RootElement, "nickname"),
                Email = ReadString(doc.RootElement, "email"),
                Picture = ReadString(doc.RootElement, "picture")
            };
        }
        catch (Exception ex)
        {
            // Provisioning must survive an Auth0 outage, so a failed lookup degrades to claim-only seeding.
            _logger.LogWarning(ex, "Auth0 userinfo lookup failed.");
            return null;
        }
    }

    private static string? ReadString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}
