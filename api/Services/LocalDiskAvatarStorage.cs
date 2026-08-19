using api.Interfaces;

namespace api.Services;

public class LocalDiskAvatarStorage : IAvatarStorage
{
    public const string RequestPath = "/uploads/avatars";
    private const long DefaultMaxBytes = 2 * 1024 * 1024;

    private readonly string _root;
    private readonly long _maxBytes;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<LocalDiskAvatarStorage> _logger;

    public LocalDiskAvatarStorage(
        IConfiguration configuration,
        IHostEnvironment environment,
        IHttpContextAccessor httpContextAccessor,
        ILogger<LocalDiskAvatarStorage> logger)
    {
        _root = ResolveRoot(configuration, environment);
        _maxBytes = configuration.GetValue<long?>("Storage:AvatarMaxBytes") ?? DefaultMaxBytes;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public static string ResolveRoot(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration["Storage:AvatarPath"];
        var path = string.IsNullOrWhiteSpace(configured) ? "uploads/avatars" : configured;
        return Path.IsPathRooted(path) ? path : Path.Combine(environment.ContentRootPath, path);
    }

    public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length == 0)
        {
            throw new ProfileValidationException("The uploaded file is empty.");
        }

        if (file.Length > _maxBytes)
        {
            throw new ProfileValidationException(
                $"Avatar images must be {_maxBytes / (1024 * 1024)} MB or smaller.");
        }

        await using var upload = file.OpenReadStream();

        var header = new byte[12];
        var read = await ReadExactlyAsync(upload, header, cancellationToken);
        var extension = SniffExtension(header, read)
            ?? throw new ProfileValidationException(
                "Avatar images must be a JPEG, PNG, or WebP file.");

        Directory.CreateDirectory(_root);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_root, fileName);

        await using (var destination = File.Create(fullPath))
        {
            await destination.WriteAsync(header.AsMemory(0, read), cancellationToken);
            await upload.CopyToAsync(destination, cancellationToken);
        }

        return $"{RequestBaseUrl()}{RequestPath}/{fileName}";
    }

    public Task DeleteAsync(string? url, CancellationToken cancellationToken = default)
    {
        var fileName = LocalFileName(url);
        if (fileName is null)
        {
            return Task.CompletedTask;
        }

        try
        {
            var fullPath = Path.Combine(_root, fileName);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Could not delete the previous avatar file {FileName}.", fileName);
        }

        return Task.CompletedTask;
    }

    private static string? LocalFileName(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var marker = $"{RequestPath}/";
        var index = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return null;
        }

        var candidate = url[(index + marker.Length)..];
        // Guard against a crafted PictureUrl walking out of the avatar directory.
        return candidate.Length > 0 && candidate == Path.GetFileName(candidate) ? candidate : null;
    }

    private static async Task<int> ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(total), cancellationToken);
            if (read == 0)
            {
                break;
            }

            total += read;
        }

        return total;
    }

    private static string? SniffExtension(byte[] header, int length)
    {
        if (length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return ".jpg";
        }

        if (length >= 8
            && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
            && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
        {
            return ".png";
        }

        if (length >= 12
            && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
            && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
        {
            return ".webp";
        }

        return null;
    }

    private string RequestBaseUrl()
    {
        var request = _httpContextAccessor.HttpContext?.Request
            ?? throw new InvalidOperationException("Avatar uploads require an active HTTP request.");
        return $"{request.Scheme}://{request.Host}";
    }
}
