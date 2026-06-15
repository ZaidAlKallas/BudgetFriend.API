using BudgetFriend.API.Database.Entites;

namespace BudgetFriend.API.Features.Authentication.Jwt;

public interface IJwtTokenGenerator
{
    (string Token, string JwtId) Generate(User user);
    string GetJwtId(string token);
}
