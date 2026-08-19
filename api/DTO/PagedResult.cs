namespace api.DTO;

public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Skip, int Take);
