namespace BudgetFriend.API.Shared.Pagination;

public record PaginationRequest(
    int PageNumber = 1,
    int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public int ValidPageSize => Math.Clamp(PageSize, 1, MaxPageSize);
    public int Skip => (Math.Max(PageNumber, 1) - 1) * ValidPageSize;
}
