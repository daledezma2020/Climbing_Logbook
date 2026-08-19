using api.DTO;
using api.Interfaces;

namespace api.Tests.Unit;

internal sealed class SocialServiceStub : ISocialService
{
    public bool TargetExists { get; set; } = true;
    public CommentDeletion DeletionResult { get; set; } = CommentDeletion.Deleted;
    public PagedResult<LogEntryDto> Feed { get; set; } = new([], 0, 0, 25);
    public int? LastFeedCallerId { get; private set; }
    public int? LastDecoratedCallerId { get; private set; }
    public (int UserId, int LogEntryId)? LastLike { get; private set; }
    public (int UserId, int LogEntryId)? LastUnlike { get; private set; }

    public Task<PagedResult<LogEntryDto>> GetFeedAsync(
        int callerId, int skip, int take, CancellationToken cancellationToken = default)
    {
        LastFeedCallerId = callerId;
        return Task.FromResult(Feed);
    }

    public Task<bool> LikeAsync(int userId, int logEntryId, CancellationToken cancellationToken = default)
    {
        LastLike = (userId, logEntryId);
        return Task.FromResult(TargetExists);
    }

    public Task<bool> UnlikeAsync(int userId, int logEntryId, CancellationToken cancellationToken = default)
    {
        LastUnlike = (userId, logEntryId);
        return Task.FromResult(TargetExists);
    }

    public Task<PagedResult<CommentDto>> GetLogEntryCommentsAsync(
        int logEntryId, int? callerId, int skip, int take, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PagedResult<CommentDto>([], 0, skip, take));

    public Task<PagedResult<CommentDto>> GetClimbCommentsAsync(
        int climbId, int? callerId, int skip, int take, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PagedResult<CommentDto>([], 0, skip, take));

    public Task<CommentDto?> AddLogEntryCommentAsync(
        int userId, int logEntryId, CreateCommentDto dto, CancellationToken cancellationToken = default) =>
        Task.FromResult(TargetExists
            ? new CommentDto { Id = 1, Content = dto.Content, LogEntryId = logEntryId }
            : null);

    public Task<CommentDto?> AddClimbCommentAsync(
        int userId, int climbId, CreateCommentDto dto, CancellationToken cancellationToken = default) =>
        Task.FromResult(TargetExists
            ? new CommentDto { Id = 1, Content = dto.Content, ClimbId = climbId }
            : null);

    public Task<CommentDeletion> DeleteCommentAsync(
        int userId, int commentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(DeletionResult);

    public Task DecorateLogEntriesAsync(
        IReadOnlyCollection<LogEntryDto> entries,
        int? callerId,
        CancellationToken cancellationToken = default)
    {
        LastDecoratedCallerId = callerId;
        return Task.CompletedTask;
    }
}
