namespace BudgetFriend.API.Features.Authentication.Google;

public sealed record GoogleUserInfo(
    string Subject,
    string? Email,
    bool EmailVerified,
    string? FirstName,
    string? LastName);