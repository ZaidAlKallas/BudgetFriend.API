namespace BudgetFriend.API.Shared.Pagination;

public record PaginationRequest(
    int PageNumber = 1,
    int PageSize = 20)
{
    public const int MaxPageSize = 100;
    public int ValidPageSize => Math.Min(PageSize, MaxPageSize);
    public int Skip => (PageNumber - 1) * ValidPageSize;
}
