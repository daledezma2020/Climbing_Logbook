namespace api.Interfaces;

public interface IAuth0UserInfoClient
{
    Task<Auth0UserInfo?> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken);
}

public class Auth0UserInfo
{
    public string? Name { get; set; }
    public string? Nickname { get; set; }
    public string? Email { get; set; }
    public string? Picture { get; set; }
}
