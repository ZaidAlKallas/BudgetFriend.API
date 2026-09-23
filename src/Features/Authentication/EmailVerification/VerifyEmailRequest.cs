namespace BudgetFriend.API.Features.Authentication.EmailVerification;

public sealed record VerifyEmailRequest(string Email, string Code);