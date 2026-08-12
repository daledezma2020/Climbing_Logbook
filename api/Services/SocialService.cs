using api.DTO;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace api.Services;

public class SocialService : ISocialService
{
    private const int MaxPageSize = 100;

    private readonly ApplicationDbContext _context;
    private readonly ICatalogService _catalogService;
    private readonly IFollowService _followService;

    public SocialService(
        ApplicationDbContext context,
        ICatalogService catalogService,
        IFollowService followService)
    {
        _context = context;
        _catalogService = catalogService;
        _followService = followService;
    }

    public async Task<PagedResult<LogEntryDto>> GetFeedAsync(
        int callerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var authorIds = await _followService.GetFollowingIdsAsync(callerId, cancellationToken);
        authorIds.Add(callerId);

        var page = await _catalogService.GetLogEntriesForUsersPagedAsync(
            authorIds, skip, take, cancellationToken);

        await DecorateLogEntriesAsync(page.Items, callerId, cancellationToken);
        return page;
    }

    public async Task<bool> LikeAsync(int userId, int logEntryId, CancellationToken cancellationToken = default)
    {
        if (!await _context.LogEntries.AnyAsync(l => l.Id == logEntryId, cancellationToken))
        {
            return false;
        }

        var alreadyLiked = await _context.Likes
            .AnyAsync(l => l.UserId == userId && l.LogEntryId == logEntryId, cancellationToken);
        if (alreadyLiked)
        {
            return true;
        }

        var like = new Like { UserId = userId, LogEntryId = logEntryId };
        _context.Likes.Add(like);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _context.Entry(like).State = EntityState.Detached;
        }

        return true;
    }

    public async Task<bool> UnlikeAsync(int userId, int logEntryId, CancellationToken cancellationToken = default)
    {
        if (!await _context.LogEntries.AnyAsync(l => l.Id == logEntryId, cancellationToken))
        {
            return false;
        }

        var like = await _context.Likes
            .FirstOrDefaultAsync(l => l.UserId == userId && l.LogEntryId == logEntryId, cancellationToken);
        if (like is null)
        {
            return true;
        }

        _context.Likes.Remove(like);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<PagedResult<CommentDto>> GetLogEntryCommentsAsync(
        int logEntryId,
        int? callerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        return GetCommentsAsync(
            _context.Comments.Where(c => c.LogEntryId == logEntryId),
            callerId,
            skip,
            take,
            cancellationToken);
    }

    public Task<PagedResult<CommentDto>> GetClimbCommentsAsync(
        int climbId,
        int? callerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        return GetCommentsAsync(
            _context.Comments.Where(c => c.ClimbId == climbId),
            callerId,
            skip,
            take,
            cancellationToken);
    }

    public async Task<CommentDto?> AddLogEntryCommentAsync(
        int userId,
        int logEntryId,
        CreateCommentDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!await _context.LogEntries.AnyAsync(l => l.Id == logEntryId, cancellationToken))
        {
            return null;
        }

        return await AddCommentAsync(
            new Comment { UserId = userId, LogEntryId = logEntryId, Content = dto.Content.Trim() },
            cancellationToken);
    }

    public async Task<CommentDto?> AddClimbCommentAsync(
        int userId,
        int climbId,
        CreateCommentDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!await _context.Climbs.AnyAsync(c => c.Id == climbId, cancellationToken))
        {
            return null;
        }

        return await AddCommentAsync(
            new Comment { UserId = userId, ClimbId = climbId, Content = dto.Content.Trim() },
            cancellationToken);
    }

    public async Task<CommentDeletion> DeleteCommentAsync(
        int userId,
        int commentId,
        CancellationToken cancellationToken = default)
    {
        var comment = await _context.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);
        if (comment is null)
        {
            return CommentDeletion.NotFound;
        }

        if (comment.UserId != userId)
        {
            return CommentDeletion.Forbidden;
        }

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync(cancellationToken);
        return CommentDeletion.Deleted;
    }

    public async Task DecorateLogEntriesAsync(
        IReadOnlyCollection<LogEntryDto> entries,
        int? callerId,
        CancellationToken cancellationToken = default)
    {
        if (entries.Count == 0)
        {
            return;
        }

        var ids = entries.Select(e => e.Id).Distinct().ToList();

        var likeCounts = await _context.Likes
            .Where(l => ids.Contains(l.LogEntryId))
            .GroupBy(l => l.LogEntryId)
            .Select(g => new { LogEntryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.LogEntryId, g => g.Count, cancellationToken);

        var commentCounts = await _context.Comments
            .Where(c => c.LogEntryId != null && ids.Contains(c.LogEntryId!.Value))
            .GroupBy(c => c.LogEntryId!.Value)
            .Select(g => new { LogEntryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.LogEntryId, g => g.Count, cancellationToken);

        var likedByCaller = callerId.HasValue
            ? (await _context.Likes
                .Where(l => l.UserId == callerId.Value && ids.Contains(l.LogEntryId))
                .Select(l => l.LogEntryId)
                .ToListAsync(cancellationToken))
                .ToHashSet()
            : [];

        foreach (var entry in entries)
        {
            entry.LikeCount = likeCounts.GetValueOrDefault(entry.Id);
            entry.CommentCount = commentCounts.GetValueOrDefault(entry.Id);
            entry.IsLikedByMe = likedByCaller.Contains(entry.Id);
        }

        var authors = entries.Select(e => e.User).OfType<UserSummaryDto>().ToList();
        await _followService.DecorateAsync(authors, callerId, cancellationToken);
    }

    private async Task<CommentDto> AddCommentAsync(Comment comment, CancellationToken cancellationToken)
    {
        _context.Comments.Add(comment);
        await _context.SaveChangesAsync(cancellationToken);

        var saved = await _context.Comments
            .Include(c => c.User)!.ThenInclude(u => u!.HomePlace)
            .FirstAsync(c => c.Id == comment.Id, cancellationToken);

        var dto = ToDto(saved, comment.UserId);
        await DecorateAuthorsAsync([dto], comment.UserId, cancellationToken);
        return dto;
    }

    private async Task<PagedResult<CommentDto>> GetCommentsAsync(
        IQueryable<Comment> query,
        int? callerId,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        skip = Math.Max(skip, 0);
        take = Math.Clamp(take, 1, MaxPageSize);

        var total = await query.CountAsync(cancellationToken);
        var comments = await query
            .Include(c => c.User)!.ThenInclude(u => u!.HomePlace)
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        var dtos = comments.Select(c => ToDto(c, callerId)).ToList();
        await DecorateAuthorsAsync(dtos, callerId, cancellationToken);

        return new PagedResult<CommentDto>(dtos, total, skip, take);
    }

    private Task DecorateAuthorsAsync(
        IReadOnlyCollection<CommentDto> comments,
        int? callerId,
        CancellationToken cancellationToken)
    {
        var authors = comments.Select(c => c.User).OfType<UserSummaryDto>().ToList();
        return _followService.DecorateAsync(authors, callerId, cancellationToken);
    }

    private static CommentDto ToDto(Comment comment, int? callerId)
    {
        return new CommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            User = comment.User is null ? null : CatalogMapping.ToSummaryDto(comment.User),
            CreatedAt = comment.CreatedAt,
            ClimbId = comment.ClimbId,
            LogEntryId = comment.LogEntryId,
            CanDelete = callerId.HasValue && callerId.Value == comment.UserId
        };
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}
