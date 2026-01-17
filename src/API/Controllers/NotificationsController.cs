using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Net.Http.Headers;

namespace ApiGateway.Controllers;

/// <summary>
/// Notification API - proxies requests to Notification Service
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationsController> _logger;
    private readonly string _notificationServiceUrl;

    public NotificationsController(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<NotificationsController> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _notificationServiceUrl = configuration["NotificationService:Url"] ?? "http://localhost:5005";
    }

    /// <summary>
    /// Get notifications for current user
    /// </summary>
    /// <param name="unreadOnly">Filter unread notifications only</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <returns>Paginated notifications with unread count</returns>
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] bool? unreadOnly = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var token = GetAuthorizationToken();
            var url = $"{_notificationServiceUrl}/api/notifications";

            var queryParams = new List<string>();
            if (unreadOnly.HasValue)
                queryParams.Add($"unreadOnly={unreadOnly.Value.ToString().ToLower()}");
            queryParams.Add($"page={page}");
            queryParams.Add($"pageSize={pageSize}");

            if (queryParams.Any())
                url += "?" + string.Join("&", queryParams);

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Notification service error: {response.StatusCode}");
                return StatusCode((int)response.StatusCode, new { message = "Failed to fetch notifications" });
            }

            var content = await response.Content.ReadAsStringAsync();
            return Ok(System.Text.Json.JsonDocument.Parse(content).RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching notifications");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Mark a single notification as read
    /// </summary>
    /// <param name="id">Notification ID</param>
    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        try
        {
            var token = GetAuthorizationToken();
            var url = $"{_notificationServiceUrl}/api/notifications/{id}/read";

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Notification service error: {response.StatusCode}");
                return StatusCode((int)response.StatusCode, new { message = "Failed to mark notification as read" });
            }

            return Ok(new { message = "Notification marked as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Mark all notifications as read for current user
    /// </summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var token = GetAuthorizationToken();
            var url = $"{_notificationServiceUrl}/api/notifications/read-all";

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent("", System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Notification service error: {response.StatusCode}");
                return StatusCode((int)response.StatusCode, new { message = "Failed to mark notifications as read" });
            }

            return Ok(new { message = "All notifications marked as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get notification templates (admin only)
    /// </summary>
    [HttpGet("templates")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> GetTemplates([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var token = GetAuthorizationToken();
            var url = $"{_notificationServiceUrl}/api/templates?page={page}&pageSize={pageSize}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, new { message = "Failed to fetch templates" });
            }

            var content = await response.Content.ReadAsStringAsync();
            return Ok(System.Text.Json.JsonDocument.Parse(content).RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching templates");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get notification preferences for current user
    /// </summary>
    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences()
    {
        try
        {
            var token = GetAuthorizationToken();
            var url = $"{_notificationServiceUrl}/api/preferences";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, new { message = "Failed to fetch preferences" });
            }

            var content = await response.Content.ReadAsStringAsync();
            return Ok(System.Text.Json.JsonDocument.Parse(content).RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching preferences");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update notification preferences for current user
    /// </summary>
    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] Dictionary<string, object> preferences)
    {
        try
        {
            var token = GetAuthorizationToken();
            var url = $"{_notificationServiceUrl}/api/preferences";

            var request = new HttpRequestMessage(HttpMethod.Put, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(preferences),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, new { message = "Failed to update preferences" });
            }

            return Ok(new { message = "Preferences updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating preferences");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    private string GetAuthorizationToken()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader))
            throw new UnauthorizedAccessException("Missing authorization header");

        // Remove "Bearer " prefix if present
        return authHeader.Replace("Bearer ", "");
    }
}
