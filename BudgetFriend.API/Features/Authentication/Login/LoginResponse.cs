namespace BudgetFriend.API.Features.Authentication.Login;

public sealed record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpireAtUtc);
