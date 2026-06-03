namespace BudgetFriend.API.Features.Authentication.Register;

public sealed record RegisterResponse(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    DateTime CreatedAtUtc);
