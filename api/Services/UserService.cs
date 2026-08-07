using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Security.Claims;
using System.Text;

namespace api.Services;

public class UserService : IUserService
{
    private const int MaxUsernameLength = 50;
    private const int MaxUsernameBaseLength = 40;
    private const string FallbackUsernameBase = "climber";

    private readonly ApplicationDbContext _context;
    private readonly IAuth0UserInfoClient _userInfoClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<UserService> _logger;

    public UserService(
        ApplicationDbContext context,
        IAuth0UserInfoClient userInfoClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<UserService> logger)
    {
        _context = context;
        _userInfoClient = userInfoClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<AppUser> EnsureUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new InvalidOperationException("The access token does not contain a subject claim.");
        }

        var existing = await _context.AppUsers
            .Include(u => u.HomePlace)
            .FirstOrDefaultAsync(u => u.Auth0Subject == subject, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var profile = await ResolveProfileAsync(principal, cancellationToken);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var user = new AppUser
            {
                Auth0Subject = subject,
                Username = await DeriveUsernameAsync(profile, cancellationToken),
                DisplayName = string.Empty,
                Email = profile.Email,
                PictureUrl = profile.Picture
            };
            user.DisplayName = Truncate(
                string.IsNullOrWhiteSpace(profile.Name) ? user.Username : profile.Name.Trim(),
                100);

            _context.AppUsers.Add(user);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Provisioned AppUser {Username} for a new Auth0 subject.", user.Username);
                return user;
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                _context.Entry(user).State = EntityState.Detached;

                var raced = await _context.AppUsers
                    .Include(u => u.HomePlace)
                    .FirstOrDefaultAsync(u => u.Auth0Subject == subject, cancellationToken);
                if (raced is not null)
                {
                    return raced;
                }
            }
        }

        throw new InvalidOperationException("Could not provision a user record after repeated username collisions.");
    }

    private async Task<Auth0UserInfo> ResolveProfileAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var profile = new Auth0UserInfo
        {
            Name = principal.FindFirstValue("name"),
            Nickname = principal.FindFirstValue("nickname"),
            Email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email"),
            Picture = principal.FindFirstValue("picture")
        };

        var complete = !string.IsNullOrWhiteSpace(profile.Name)
            && !string.IsNullOrWhiteSpace(profile.Email)
            && !string.IsNullOrWhiteSpace(profile.Picture);
        if (complete)
        {
            return profile;
        }

        var accessToken = ReadBearerToken();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return profile;
        }

        var userInfo = await _userInfoClient.GetUserInfoAsync(accessToken, cancellationToken);
        if (userInfo is null)
        {
            return profile;
        }

        profile.Name = Coalesce(profile.Name, userInfo.Name);
        profile.Nickname = Coalesce(profile.Nickname, userInfo.Nickname);
        profile.Email = Coalesce(profile.Email, userInfo.Email);
        profile.Picture = Coalesce(profile.Picture, userInfo.Picture);
        return profile;
    }

    private string? ReadBearerToken()
    {
        var header = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return header["Bearer ".Length..].Trim();
    }

    private async Task<string> DeriveUsernameAsync(Auth0UserInfo profile, CancellationToken cancellationToken)
    {
        var emailLocalPart = profile.Email?.Split('@', 2)[0];
        var candidate = Sanitize(emailLocalPart)
            ?? Sanitize(profile.Nickname)
            ?? Sanitize(profile.Name)
            ?? FallbackUsernameBase;

        var suffix = 1;
        var attempt = candidate;
        while (await UsernameTakenAsync(attempt, cancellationToken))
        {
            suffix++;
            attempt = Truncate(candidate, MaxUsernameLength - suffix.ToString().Length) + suffix;
        }

        return attempt;
    }

    private Task<bool> UsernameTakenAsync(string username, CancellationToken cancellationToken)
    {
        var normalized = username.ToLowerInvariant();
        return _context.AppUsers.AnyAsync(u => u.Username.ToLower() == normalized, cancellationToken);
    }

    private static string? Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value.ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                builder.Append(character);
            }
            else if (character is '.' or '_' or '-' or ' ')
            {
                var separator = character == ' ' ? '-' : character;
                if (builder.Length > 0 && builder[^1] != separator)
                {
                    builder.Append(separator);
                }
            }
        }

        var sanitized = builder.ToString().Trim('.', '_', '-');
        sanitized = Truncate(sanitized, MaxUsernameBaseLength).TrimEnd('.', '_', '-');
        return string.IsNullOrEmpty(sanitized) ? null : sanitized;
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string? Coalesce(string? preferred, string? fallback)
    {
        return string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}
