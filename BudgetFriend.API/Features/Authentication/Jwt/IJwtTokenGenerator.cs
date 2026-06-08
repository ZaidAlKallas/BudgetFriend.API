using BudgetFriend.API.Database.Entites;

namespace BudgetFriend.API.Features.Authentication.Jwt;

public interface IJwtTokenGenerator
{
    string Generate(User user);
}
