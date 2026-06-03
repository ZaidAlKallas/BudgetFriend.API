namespace BudgetFriend.API.Features.Authentication.Register;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string? FirstName,
    string? LastName);
