namespace api.Interfaces;

public interface IAvatarStorage
{
    Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default);
    Task DeleteAsync(string? url, CancellationToken cancellationToken = default);
}
