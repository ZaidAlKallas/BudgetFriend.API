using BudgetFriend.API.Database.Entites;
using BudgetFriend.API.Features.Accounts;
using BudgetFriend.API.Features.Authentication;
using BudgetFriend.API.Features.Authentication.Register;
using BudgetFriend.API.Features.Categories;
using BudgetFriend.API.Features.Dashboards;
using BudgetFriend.API.Features.Transactions;
using BudgetFriend.API.Shared.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/budgetfriend-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("Application", "BudgetFriend")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddOpenApi();

builder.Services.AddValidatorsFromAssemblyContaining<RegisterValidator>();
builder.Services.AddDatabase(builder.Configuration);

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddAuthServices(builder.Configuration);

builder.Services.AddLoginRateLimiting();

builder.Services.AddHttpContextAccessor()
    .AddScoped<ICurrentUser, CurrentUser>();

var app = builder.Build();

app.ConfigurePipline();

app.MapAuthenticationEndpoints();
app.MapAccountEndpoints();
app.MapCategoryEndpoints();
app.MapTransactionEndpoints();
app.MapDashboardEndpoints();

await app.RunAsync();

