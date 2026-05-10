using System.Net.Http.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.API.Common;
using SchoolManagement.API.Realtime.Hubs;
using SchoolManagement.Application.Authentication;
using SchoolManagement.Application.Notifications;
using SchoolManagement.Tests.Infrastructure;

namespace SchoolManagement.Tests.Realtime;

public sealed class SignalRIntegrationTests : IClassFixture<SchoolManagementApiFactory>
{
    private readonly SchoolManagementApiFactory _factory;

    public SignalRIntegrationTests(SchoolManagementApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NotificationHub_without_token_rejects_connection()
    {
        var connection = CreateConnection("/hubs/notifications");

        await Assert.ThrowsAnyAsync<Exception>(() => connection.StartAsync());

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task AttendanceHub_without_token_rejects_connection()
    {
        var connection = CreateConnection("/hubs/attendance");

        await Assert.ThrowsAnyAsync<Exception>(() => connection.StartAsync());

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task NotificationHub_with_valid_token_connects_successfully()
    {
        var token = await GetAccessTokenAsync();
        var connection = CreateConnection("/hubs/notifications", token);

        await connection.StartAsync();

        Assert.Equal(HubConnectionState.Connected, connection.State);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task AttendanceHub_with_valid_token_connects_successfully()
    {
        var token = await GetAccessTokenAsync();
        var connection = CreateConnection("/hubs/attendance", token);

        await connection.StartAsync();

        Assert.Equal(HubConnectionState.Connected, connection.State);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task Notification_event_is_delivered_only_to_target_user()
    {
        var unique = Guid.NewGuid().ToString("N");
        var userAEmail = $"signalr.user.a.{unique}@school.com";
        var userBEmail = $"signalr.user.b.{unique}@school.com";
        const string password = "Student@123";

        await _factory.SeedUserAsync("Student", userAEmail, password, "SignalR User A");
        await _factory.SeedUserAsync("Student", userBEmail, password, "SignalR User B");

        var userAId = await _factory.ExecuteDbContextAsync(db =>
            db.Users.Where(x => x.Email == userAEmail).Select(x => x.Id).SingleAsync());

        var userBId = await _factory.ExecuteDbContextAsync(db =>
            db.Users.Where(x => x.Email == userBEmail).Select(x => x.Id).SingleAsync());

        Assert.NotEqual(userAId, userBId);

        var userAToken = await GetAccessTokenAsync(userAEmail, password);
        var userBToken = await GetAccessTokenAsync(userBEmail, password);

        var userAReceived = new TaskCompletionSource<NotificationRealtimeEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        var userBReceived = new TaskCompletionSource<NotificationRealtimeEvent>(TaskCreationOptions.RunContinuationsAsynchronously);

        var userAConnection = CreateConnection("/hubs/notifications", userAToken);
        var userBConnection = CreateConnection("/hubs/notifications", userBToken);

        userAConnection.On<NotificationRealtimeEvent>(
            NotificationHub.NotificationEventMethod,
            payload => userAReceived.TrySetResult(payload));

        userBConnection.On<NotificationRealtimeEvent>(
            NotificationHub.NotificationEventMethod,
            payload => userBReceived.TrySetResult(payload));

        await userAConnection.StartAsync();
        await userBConnection.StartAsync();

        try
        {
            var notificationId = Guid.NewGuid();

            var payload = new NotificationRealtimeEvent(
                NotificationRealtimeEventTypes.Created,
                new NotificationResponse(
                    notificationId,
                    userAId,
                    "Targeted SignalR Test",
                    "Only user A should receive this notification.",
                    false,
                    DateTime.UtcNow),
                new[] { notificationId },
                1,
                DateTime.UtcNow);

            using var scope = _factory.Services.CreateScope();
            var notifier = scope.ServiceProvider.GetRequiredService<INotificationRealtimeNotifier>();

            await notifier.BroadcastNotificationEventAsync(userAId, payload, CancellationToken.None);

            var received = await WaitForEventAsync(userAReceived.Task);

            Assert.Equal(NotificationRealtimeEventTypes.Created, received.EventType);
            Assert.NotNull(received.Notification);
            Assert.Equal(userAId, received.Notification.UserId);
            Assert.Contains(notificationId, received.NotificationIds);

            var userBGotEvent = await EventArrivedAsync(userBReceived.Task, TimeSpan.FromMilliseconds(500));
            Assert.False(userBGotEvent, "User B received a notification that was targeted only to User A.");
        }
        finally
        {
            await userAConnection.DisposeAsync();
            await userBConnection.DisposeAsync();
        }
    }

    private HubConnection CreateConnection(string path, string? accessToken = null)
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(new Uri("http://localhost"), path), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();

                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
                }
            })
            .Build();
    }

    private async Task<string> GetAccessTokenAsync(
        string email = SchoolManagementApiFactory.AdminEmail,
        string password = SchoolManagementApiFactory.AdminPassword)
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();

        Assert.True(
            TryGetCookieValue(response, AuthCookieNames.AccessToken, out var accessToken),
            "Login response did not include the access-token cookie.");

        Assert.False(string.IsNullOrWhiteSpace(accessToken));

        return accessToken;
    }

    private static async Task<NotificationRealtimeEvent> WaitForEventAsync(Task<NotificationRealtimeEvent> eventTask)
    {
        var completed = await Task.WhenAny(eventTask, Task.Delay(TimeSpan.FromSeconds(3)));

        Assert.True(completed == eventTask, "Expected SignalR event was not received within timeout.");

        return await eventTask;
    }

    private static async Task<bool> EventArrivedAsync(Task<NotificationRealtimeEvent> eventTask, TimeSpan timeout)
    {
        var completed = await Task.WhenAny(eventTask, Task.Delay(timeout));
        return completed == eventTask;
    }

    private static bool TryGetCookieValue(HttpResponseMessage response, string cookieName, out string cookieValue)
    {
        cookieValue = string.Empty;

        foreach (var header in response.Headers.TryGetValues("Set-Cookie", out var values) ? values : [])
        {
            var prefix = $"{cookieName}=";
            var segment = header.Split(';', 2)[0];

            if (!segment.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            cookieValue = segment[prefix.Length..];
            return true;
        }

        return false;
    }
}
