using api.DTO;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace api.Services;

public partial class UserService : IUserService
{
    private const int MaxUsernameLength = 50;
    private const int MinUsernameLength = 3;
    private const int MaxUsernameBaseLength = 40;
    private const string FallbackUsernameBase = "climber";
    private static readonly HashSet<string> ReservedUsernames = new(StringComparer.OrdinalIgnoreCase)
    {
        "me", "profile", "profiles", "users", "admin", "settings", "new", "edit", "login", "logout"
    };

    [GeneratedRegex("^[a-z0-9]([a-z0-9_-]*[a-z0-9])?$")]
    private static partial Regex UsernamePattern();

    private readonly ApplicationDbContext _context;
    private readonly IAuth0UserInfoClient _userInfoClient;
    private readonly ICatalogService _catalogService;
    private readonly IAvatarStorage _avatarStorage;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<UserService> _logger;

    public UserService(
        ApplicationDbContext context,
        IAuth0UserInfoClient userInfoClient,
        ICatalogService catalogService,
        IAvatarStorage avatarStorage,
        IHttpContextAccessor httpContextAccessor,
        ILogger<UserService> logger)
    {
        _context = context;
        _userInfoClient = userInfoClient;
        _catalogService = catalogService;
        _avatarStorage = avatarStorage;
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

    public Task<AppUser?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = (username ?? string.Empty).Trim().ToLowerInvariant();
        return _context.AppUsers
            .Include(u => u.HomePlace)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalized, cancellationToken);
    }

    public async Task<UserProfileDto> GetProfileAsync(
        AppUser user,
        bool includePrivate,
        CancellationToken cancellationToken = default)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Email = includePrivate ? user.Email : null,
            Bio = user.Bio,
            PictureUrl = user.PictureUrl,
            HomePlaceId = user.HomePlaceId,
            HomePlace = user.HomePlace == null ? null : CatalogMapping.ToDto(user.HomePlace),
            CreatedAt = user.CreatedAt,
            Stats = await _catalogService.GetUserStatsAsync(user.Id, cancellationToken)
        };
    }

    public async Task<AppUser> UpdateProfileAsync(
        int userId,
        UpdateUserProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.AppUsers
            .Include(u => u.HomePlace)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new ProfileValidationException("The signed-in user no longer exists.");

        var username = (dto.Username ?? string.Empty).Trim();
        ValidateUsername(username);

        if (!string.Equals(username, user.Username, StringComparison.OrdinalIgnoreCase))
        {
            var normalized = username.ToLowerInvariant();
            var taken = await _context.AppUsers
                .AnyAsync(u => u.Id != userId && u.Username.ToLower() == normalized, cancellationToken);
            if (taken)
            {
                throw new UsernameConflictException(username);
            }
        }

        Place? homePlace = null;
        if (dto.HomePlaceId.HasValue)
        {
            homePlace = await _context.Places
                .FirstOrDefaultAsync(p => p.Id == dto.HomePlaceId.Value, cancellationToken)
                ?? throw new ProfileValidationException("The selected home place does not exist.");
        }

        var displayName = (dto.DisplayName ?? string.Empty).Trim();
        if (displayName.Length == 0)
        {
            throw new ProfileValidationException("Enter a display name.");
        }

        user.Username = username;
        user.DisplayName = displayName;
        user.Bio = string.IsNullOrWhiteSpace(dto.Bio) ? null : dto.Bio.Trim();
        user.PictureUrl = string.IsNullOrWhiteSpace(dto.PictureUrl) ? null : dto.PictureUrl.Trim();
        user.HomePlaceId = dto.HomePlaceId;
        user.HomePlace = homePlace;
        user.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Backstop for a concurrent claim of the same username; the DB owns the
            // case-insensitive guarantee via IX_AppUsers_Username_Lower.
            throw new UsernameConflictException(username);
        }

        return user;
    }

    public async Task<AppUser> SetAvatarAsync(int userId, IFormFile file, CancellationToken cancellationToken = default)
    {
        var user = await _context.AppUsers
            .Include(u => u.HomePlace)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new ProfileValidationException("The signed-in user no longer exists.");

        var previous = user.PictureUrl;
        user.PictureUrl = await _avatarStorage.SaveAsync(file, cancellationToken);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        await _avatarStorage.DeleteAsync(previous, cancellationToken);
        return user;
    }

    private static void ValidateUsername(string username)
    {
        if (username.Length is < MinUsernameLength or > MaxUsernameLength)
        {
            throw new ProfileValidationException(
                $"Usernames must be between {MinUsernameLength} and {MaxUsernameLength} characters.");
        }

        if (!UsernamePattern().IsMatch(username))
        {
            throw new ProfileValidationException(
                "Usernames may only use lowercase letters, numbers, hyphens, and underscores, and must start and end with a letter or number.");
        }

        if (ReservedUsernames.Contains(username))
        {
            throw new ProfileValidationException($"\"{username}\" is reserved and cannot be used as a username.");
        }
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
