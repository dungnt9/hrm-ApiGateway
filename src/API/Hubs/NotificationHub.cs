using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ApiGateway.Hubs;

/// <summary>
/// SignalR Hub for real-time notifications
/// Proxies notifications from backend services to connected clients
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"User {userId} connected to notification hub");

        // Add user to a group for targeted notifications
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"User {userId} disconnected from notification hub");

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client method: Mark a notification as read
    /// </summary>
    public async Task MarkAsRead(string notificationId)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation($"User {userId} marked notification {notificationId} as read");

            // Send acknowledgment to client
            await Clients.Caller.SendAsync("NotificationMarkedAsRead", notificationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read");
        }
    }

    /// <summary>
    /// Client method: Get all unread notifications for current user
    /// This method delegates to the HTTP API
    /// </summary>
    public async Task GetUnreadNotifications()
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation($"User {userId} requested unread notifications");

            // In a real scenario, you'd fetch from the database here
            // For now, just acknowledge the request
            await Clients.Caller.SendAsync("UnreadNotificationsRequested");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread notifications");
        }
    }

    /// <summary>
    /// Server method: Send a notification to a specific user
    /// Called by backend services via HTTP or message queue
    /// </summary>
    public async Task SendNotificationToUser(string userId, string title, string message, string type, string? data = null)
    {
        try
        {
            _logger.LogInformation($"Sending notification to user {userId}");

            await Clients.Group($"user_{userId}").SendAsync(
                "ReceiveNotification",
                new
                {
                    Title = title,
                    Message = message,
                    Type = type,
                    Data = data,
                    Timestamp = DateTime.UtcNow
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification");
        }
    }

    /// <summary>
    /// Server method: Broadcast notification to all connected users
    /// </summary>
    public async Task BroadcastNotification(string title, string message, string type, string? data = null)
    {
        try
        {
            _logger.LogInformation("Broadcasting notification to all users");

            await Clients.All.SendAsync(
                "ReceiveNotification",
                new
                {
                    Title = title,
                    Message = message,
                    Type = type,
                    Data = data,
                    Timestamp = DateTime.UtcNow
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting notification");
        }
    }

    private string? GetCurrentUserId()
    {
        return Context?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
