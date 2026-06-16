namespace BudgetFriend.API.IntegrationTests.CustomWebApplicationFactory;

public static class ApiRoutes
{
    public static class Auth
    {
        private const string _prefix = "/api/auth";
        public const string Register = _prefix + "/register";
        public const string Login = _prefix + "/login";
        public const string Refresh = _prefix + "/refresh";
        public const string Logout = _prefix + "/logout";
        public const string Profile = _prefix + "/profile";
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
