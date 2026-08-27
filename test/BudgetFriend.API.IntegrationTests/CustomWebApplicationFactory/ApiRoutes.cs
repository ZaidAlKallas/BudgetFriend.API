namespace BudgetFriend.API.IntegrationTests.CustomWebApplicationFactory;

public static class ApiRoutes
{
    public static class Auth
    {
        private const string _prefix = "/api/v1/auth";
        public const string Register = _prefix + "/register";
        public const string Login = _prefix + "/login";
        public const string Refresh = _prefix + "/refresh";
        public const string Logout = _prefix + "/logout";
        public const string Profile = _prefix + "/profile";
    }

    public static class Accounts
    {
        public const string Base = "/api/v1/accounts";
        public static string ById(Guid id) => $"{Base}/{id}";
    }

    public static class Categories
    {
        public const string Base = "/api/v1/categories";
        public static string ById(Guid id) => $"{Base}/{id}";
    }

    public static class Transactions
    {
        public const string Base = "/api/v1/transactions";
        public static string ById(Guid id) => $"{Base}/{id}";
    }

    public static class Dashboard
    {
        public const string Base = "/api/v1/dashboard";
        public static string Summary = "/api/v1/dashboard/summary";
        public static string CategoriesAnalysis = "/api/v1/dashboard/categories-analysis";
    }

    public static class Transfers
    {
        public const string Base = "/api/v1/transfers";
        public static string ById(Guid id) => $"{Base}/{id}";
    }
}
