using BudgetFriend.API.Features.Dashboards.GetCategoriesAnalysis;
using BudgetFriend.API.Features.Dashboards.GetDashboard;
using BudgetFriend.API.Features.Dashboards.GetSummary;

namespace BudgetFriend.API.Features.Dashboards;

public static class DashboardEndpoints
{
    public static WebApplication MapDashboardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/dashboard")
            .RequireAuthorization()
            .WithTags("Dashboard")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGetDashboard();
        group.MapGetSummary();
        group.MapGetCategoriesAnalysis();

        return app;
    }
}
