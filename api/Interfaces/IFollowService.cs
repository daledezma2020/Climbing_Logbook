using api.DTO;

namespace api.Interfaces;

public record FollowCounts(int Followers, int Following);

public interface IFollowService
{
    Task FollowAsync(int followerId, int followeeId, CancellationToken cancellationToken = default);

    Task UnfollowAsync(int followerId, int followeeId, CancellationToken cancellationToken = default);

    Task<FollowCounts> GetCountsAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> IsFollowingAsync(int followerId, int followeeId, CancellationToken cancellationToken = default);

    Task<HashSet<int>> GetFollowedIdsAsync(
        int followerId,
        IReadOnlyCollection<int> candidateIds,
        CancellationToken cancellationToken = default);

    Task<List<int>> GetFollowingIdsAsync(int followerId, CancellationToken cancellationToken = default);

    Task<PagedResult<UserSummaryDto>> GetFollowersAsync(
        int userId,
        int? callerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<PagedResult<UserSummaryDto>> GetFollowingAsync(
        int userId,
        int? callerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task DecorateAsync(
        IReadOnlyCollection<UserSummaryDto> users,
        int? callerId,
        CancellationToken cancellationToken = default);
}
