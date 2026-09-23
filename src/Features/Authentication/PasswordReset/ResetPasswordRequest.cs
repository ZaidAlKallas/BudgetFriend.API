namespace BudgetFriend.API.Features.Authentication.PasswordReset;

public sealed record ResetPasswordRequest(string Email, string Code, string NewPassword);