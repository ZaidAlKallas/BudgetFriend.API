namespace BudgetFriend.API.Features.Authentication.Login;

public sealed record LoginResponse(string AccessToken, DateTime ExpireAtUtc);
