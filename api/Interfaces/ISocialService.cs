using api.DTO;

namespace api.Interfaces;

public interface ISocialService
{
    Task<PagedResult<LogEntryDto>> GetFeedAsync(
        int callerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<bool> LikeAsync(int userId, int logEntryId, CancellationToken cancellationToken = default);

    Task<bool> UnlikeAsync(int userId, int logEntryId, CancellationToken cancellationToken = default);

    Task<PagedResult<CommentDto>> GetLogEntryCommentsAsync(
        int logEntryId,
        int? callerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<PagedResult<CommentDto>> GetClimbCommentsAsync(
        int climbId,
        int? callerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<CommentDto?> AddLogEntryCommentAsync(
        int userId,
        int logEntryId,
        CreateCommentDto dto,
        CancellationToken cancellationToken = default);

    Task<CommentDto?> AddClimbCommentAsync(
        int userId,
        int climbId,
        CreateCommentDto dto,
        CancellationToken cancellationToken = default);

    Task<CommentDeletion> DeleteCommentAsync(
        int userId,
        int commentId,
        CancellationToken cancellationToken = default);

    Task DecorateLogEntriesAsync(
        IReadOnlyCollection<LogEntryDto> entries,
        int? callerId,
        CancellationToken cancellationToken = default);
}

public enum CommentDeletion
{
    Deleted,
    NotFound,
    Forbidden
}
