using BudgetFriend.API.Database.Enums;
using BudgetFriend.API.Features.Accounts.Create;
using BudgetFriend.API.Features.Authentication.Login;
using BudgetFriend.API.Features.Authentication.Register;
using BudgetFriend.API.IntegrationTests.CustomWebApplicationFactory;
using BudgetFriend.API.Shared.Observability;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;

namespace BudgetFriend.API.IntegrationTests.Observability;

[Collection("IntegrationTests")]
public sealed class ObservabilityTests(BudgetFriendApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task OpenTelemetryProviders_AreRegistered()
    {
        using var scope = factory.Services.CreateScope();

        var tracerProvider = scope.ServiceProvider.GetService<TracerProvider>();
        var meterProvider = scope.ServiceProvider.GetService<MeterProvider>();

        tracerProvider.Should().NotBeNull();
        meterProvider.Should().NotBeNull();
    }

    [Fact]
    public async Task CreatingAccount_Increments_AccountsCreatedMetric()
    {
        var created = 0L;
        var instrumentFound = new TaskCompletionSource<Counter<long>>();

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == BudgetFriendMetrics.MeterName
                && instrument.Name == "budgetfriend.accounts.created")
            {
                instrumentFound.TrySetResult((Counter<long>)instrument);
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, state) =>
        {
            if (instrument.Name == "budgetfriend.accounts.created")
                Interlocked.Add(ref created, value);
        });
        listener.Start();

        var email = $"otel-metric-{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync(ApiRoutes.Auth.Register, new RegisterRequest(email, "Password1!", null, null));
        var loginResponse = await _client.PostAsJsonAsync(ApiRoutes.Auth.Login, new LoginRequest(email, "Password1!"));
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        _client.DefaultRequestHeaders.Authorization = new("Bearer", login!.AccessToken);

        var response = await _client.PostAsJsonAsync(ApiRoutes.Accounts.Base,
            new CreateAccountRequest("Observability Account", 100m, Currency.USD));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var instrument = await instrumentFound.Task.WaitAsync(TimeSpan.FromSeconds(5));
        instrument.Should().NotBeNull();
        created.Should().Be(1);
    }
}