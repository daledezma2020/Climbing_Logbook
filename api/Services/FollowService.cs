using api.DTO;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Linq.Expressions;

namespace api.Services;

public class FollowService : IFollowService
{
    private const int MaxPageSize = 100;

    private readonly ApplicationDbContext _context;

    public FollowService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task FollowAsync(int followerId, int followeeId, CancellationToken cancellationToken = default)
    {
        if (followerId == followeeId)
        {
            throw new SelfFollowException();
        }

        var alreadyFollowing = await _context.Follows
            .AnyAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId, cancellationToken);
        if (alreadyFollowing)
        {
            return;
        }

        var follow = new Follow { FollowerId = followerId, FolloweeId = followeeId };
        _context.Follows.Add(follow);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _context.Entry(follow).State = EntityState.Detached;
        }
    }

    public async Task UnfollowAsync(int followerId, int followeeId, CancellationToken cancellationToken = default)
    {
        var follow = await _context.Follows
            .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId, cancellationToken);
        if (follow is null)
        {
            return;
        }

        _context.Follows.Remove(follow);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<FollowCounts> GetCountsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var followers = await _context.Follows.CountAsync(f => f.FolloweeId == userId, cancellationToken);
        var following = await _context.Follows.CountAsync(f => f.FollowerId == userId, cancellationToken);
        return new FollowCounts(followers, following);
    }

    public Task<bool> IsFollowingAsync(int followerId, int followeeId, CancellationToken cancellationToken = default)
    {
        return _context.Follows
            .AnyAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId, cancellationToken);
    }

    public async Task<HashSet<int>> GetFollowedIdsAsync(
        int followerId,
        IReadOnlyCollection<int> candidateIds,
        CancellationToken cancellationToken = default)
    {
        if (candidateIds.Count == 0)
        {
            return [];
        }

        var followed = await _context.Follows
            .Where(f => f.FollowerId == followerId && candidateIds.Contains(f.FolloweeId))
            .Select(f => f.FolloweeId)
            .ToListAsync(cancellationToken);

        return followed.ToHashSet();
    }

    public Task<PagedResult<UserSummaryDto>> GetFollowersAsync(
        int userId,
        int? callerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        return GetPageAsync(
            _context.Follows.Where(f => f.FolloweeId == userId),
            f => f.FollowerId,
            callerId,
            skip,
            take,
            cancellationToken);
    }

    public Task<PagedResult<UserSummaryDto>> GetFollowingAsync(
        int userId,
        int? callerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        return GetPageAsync(
            _context.Follows.Where(f => f.FollowerId == userId),
            f => f.FolloweeId,
            callerId,
            skip,
            take,
            cancellationToken);
    }

    public async Task DecorateAsync(
        IReadOnlyCollection<UserSummaryDto> users,
        int? callerId,
        CancellationToken cancellationToken = default)
    {
        if (users.Count == 0)
        {
            return;
        }

        var ids = users.Select(u => u.Id).ToList();

        var followerCounts = await _context.Follows
            .Where(f => ids.Contains(f.FolloweeId))
            .GroupBy(f => f.FolloweeId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.UserId, g => g.Count, cancellationToken);

        var followedByCaller = callerId.HasValue
            ? await GetFollowedIdsAsync(callerId.Value, ids, cancellationToken)
            : [];

        foreach (var user in users)
        {
            user.FollowerCount = followerCounts.GetValueOrDefault(user.Id);
            user.IsFollowedByMe = followedByCaller.Contains(user.Id);
            user.IsMe = callerId == user.Id;
        }
    }

    // Paged in two steps because EF cannot apply Include across the Follow -> AppUser
    // projection: the ordered page of user ids is read first, then those users are loaded.
    private async Task<PagedResult<UserSummaryDto>> GetPageAsync(
        IQueryable<Follow> follows,
        Expression<Func<Follow, int>> selectUserId,
        int? callerId,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        skip = Math.Max(skip, 0);
        take = Math.Clamp(take, 1, MaxPageSize);

        var total = await follows.CountAsync(cancellationToken);
        var orderedIds = await follows
            .OrderByDescending(f => f.CreatedAt)
            .ThenByDescending(f => f.Id)
            .Skip(skip)
            .Take(take)
            .Select(selectUserId)
            .ToListAsync(cancellationToken);

        var users = await _context.AppUsers
            .Include(u => u.HomePlace)
            .Where(u => orderedIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        var byId = users.ToDictionary(u => u.Id);
        var summaries = orderedIds
            .Select(id => byId.TryGetValue(id, out var user) ? CatalogMapping.ToSummaryDto(user) : null)
            .OfType<UserSummaryDto>()
            .ToList();

        await DecorateAsync(summaries, callerId, cancellationToken);

        return new PagedResult<UserSummaryDto>(summaries, total, skip, take);
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}
