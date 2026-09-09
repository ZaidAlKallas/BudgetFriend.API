using BudgetFriend.API.Features.Authentication.Google;
using BudgetFriend.API.IntegrationTests.CustomWebApplicationFactory;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;

namespace BudgetFriend.API.IntegrationTests.Errors;

[Collection("IntegrationTests")]
public sealed class GlobalErrorHandlingTests(BudgetFriendApiFactory factory)
{
    [Fact]
    public async Task UnexpectedException_ShouldReturn500ProblemDetails_WithoutInternalDetails()
    {
        using var derivedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IGoogleIdTokenValidator>();
                services.AddSingleton<IGoogleIdTokenValidator, ThrowingGoogleIdTokenValidator>();
            }));

        var response = await derivedFactory.CreateClient()
            .PostAsJsonAsync(ApiRoutes.Auth.Google, new GoogleLoginRequest("some-id-token"));

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status500InternalServerError);
        problem.Title.Should().NotBeNullOrWhiteSpace();
        problem.Extensions.Should().ContainKey("traceId");

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("Unexpected Google ID token validation failure.");
        body.Should().NotContain("StackTrace");
    }

    [Fact]
    public async Task KnownException_BadHttpRequest_ShouldReturn400ProblemDetails()
    {
        using var derivedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IGoogleIdTokenValidator>();
                services.AddSingleton<IGoogleIdTokenValidator, ThrowingBadRequestGoogleIdTokenValidator>();
            }));

        var response = await derivedFactory.CreateClient()
            .PostAsJsonAsync(ApiRoutes.Auth.Google, new GoogleLoginRequest("some-id-token"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Title.Should().Be("Bad Request");
    }
}
