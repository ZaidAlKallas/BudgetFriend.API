namespace BudgetFriend.API.IntegrationTests.CustomWebApplicationFactory;

public static class ApiRoutes
{
    public static class Auth
    {
        private const string Prefix = "/api/auth";
        public const string Register = "/api/auth/register";
        public const string Login = "/api/auth/login";
        public const string Profile = "/api/auth/profile";
    }

    public static class Accounts
    {
        public const string Base = "/api/accounts";
        public static string ById(Guid id) => $"{Base}/{id}";
    }

    public static class Categories
    {
        public const string Base = "/api/categories";
        public static string ById(Guid id) => $"{Base}/{id}";
    }

    public static class Transactions
    {
        public const string Base = "/api/transactions";
        public static string ById(Guid id) => $"{Base}/{id}";
    }

    public static class Dashboard
    {
        public const string Base = "/api/dashboard";
        public const string Summary = "/api/dashboard/summary";
    }
}
