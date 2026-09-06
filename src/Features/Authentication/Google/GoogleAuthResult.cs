namespace BudgetFriend.API.Features.Authentication.Google;

public sealed record GoogleAuthResult(bool Success, User? User, string? Error);