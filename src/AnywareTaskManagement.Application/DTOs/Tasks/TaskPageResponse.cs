namespace AnywareTaskManagement.Application.DTOs.Tasks;

public sealed class TaskPageResponse
{
    public IReadOnlyList<TaskResponse> Items { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
}
