using BudgetFriend.API.Features.Categories.Create;
using BudgetFriend.API.Features.Categories.Delete;
using BudgetFriend.API.Features.Categories.GetAll;
using BudgetFriend.API.Features.Categories.GetById;
using BudgetFriend.API.Features.Categories.Update;

namespace BudgetFriend.API.Features.Categories;

public static class CategoryEndpoints
{
    public static WebApplication MapCategoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/categories")
            .RequireAuthorization()
            .WithTags("Categories")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapCreateCategory();
        group.MapGetAllCategories();
        group.MapGetCategoryById();
        group.MapUpdateCategory();
        group.MapDeleteCategory();

        return app;
    }
}
