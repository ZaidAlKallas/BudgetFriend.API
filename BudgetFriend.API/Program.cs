using BudgetFriend.API.Database.Entites;
using BudgetFriend.API.Features.Authentication;
using BudgetFriend.API.Features.Authentication.Register;
using BudgetFriend.API.Shared.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddValidatorsFromAssemblyContaining<RegisterValidator>();
builder.Services.AddDatabase(builder.Configuration);

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddAuthServices(builder.Configuration);

builder.Services.AddLoginRateLimiting();

builder.Services.AddHttpContextAccessor()
    .AddScoped<ICurrentUser, CurrentUser>();

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.ConfigureRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthenticationEndpoints();

await app.RunAsync();

