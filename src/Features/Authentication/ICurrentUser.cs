namespace BudgetFriend.API.Features.Authentication;

public interface ICurrentUser
{
    Guid UserId { get; }
}
