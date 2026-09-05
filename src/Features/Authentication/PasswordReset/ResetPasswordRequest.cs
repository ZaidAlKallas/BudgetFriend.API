namespace BudgetFriend.API.Features.Authentication.PasswordReset;

public sealed record ResetPasswordRequest(string Token, string NewPassword);