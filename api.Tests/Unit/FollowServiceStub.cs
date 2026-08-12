using api.DTO;
using api.Interfaces;
using api.Services;

namespace api.Tests.Unit;

internal sealed class FollowServiceStub : IFollowService
{
    public bool RejectSelfFollow { get; set; } = true;
    public FollowCounts Counts { get; set; } = new(0, 0);
    public bool Following { get; set; }
    public PagedResult<UserSummaryDto> Page { get; set; } = new([], 0, 0, 25);
    public (int Follower, int Followee)? LastFollow { get; private set; }
    public (int Follower, int Followee)? LastUnfollow { get; private set; }
    public int? LastCallerId { get; private set; }
    public int LastSkip { get; private set; }
    public int LastTake { get; private set; }

    public Task FollowAsync(int followerId, int followeeId, CancellationToken cancellationToken = default)
    {
        if (RejectSelfFollow && followerId == followeeId)
        {
            throw new SelfFollowException();
        }

        LastFollow = (followerId, followeeId);
        return Task.CompletedTask;
    }

    public Task UnfollowAsync(int followerId, int followeeId, CancellationToken cancellationToken = default)
    {
        LastUnfollow = (followerId, followeeId);
        return Task.CompletedTask;
    }

    public Task<FollowCounts> GetCountsAsync(int userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Counts);

    public Task<bool> IsFollowingAsync(int followerId, int followeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Following);

    public Task<HashSet<int>> GetFollowedIdsAsync(
        int followerId,
        IReadOnlyCollection<int> candidateIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Following ? candidateIds.ToHashSet() : []);

    public List<int> FollowingIds { get; set; } = [];

    public Task<List<int>> GetFollowingIdsAsync(int followerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(FollowingIds);

    public Task<PagedResult<UserSummaryDto>> GetFollowersAsync(
        int userId,
        int? callerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        RecordPage(callerId, skip, take);

    public Task<PagedResult<UserSummaryDto>> GetFollowingAsync(
        int userId,
        int? callerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        RecordPage(callerId, skip, take);

    public Task DecorateAsync(
        IReadOnlyCollection<UserSummaryDto> users,
        int? callerId,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    private Task<PagedResult<UserSummaryDto>> RecordPage(int? callerId, int skip, int take)
    {
        LastCallerId = callerId;
        LastSkip = skip;
        LastTake = take;
        return Task.FromResult(Page);
    }
}
