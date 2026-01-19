using ApiGateway.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<AuthController> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var keycloakUrl = _configuration["Keycloak:Authority"] ?? "http://localhost:8080/realms/hrm";
            var clientId = _configuration["Keycloak:ClientId"] ?? "hrm-frontend";
            var clientSecret = _configuration["Keycloak:ClientSecret"] ?? "";

            var tokenEndpoint = $"{keycloakUrl}/protocol/openid-connect/token";

            var client = _httpClientFactory.CreateClient();

            var requestBody = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "client_id", clientId },
                { "username", dto.Username },
                { "password", dto.Password },
                { "scope", "openid profile email" }
            };

            if (!string.IsNullOrEmpty(clientSecret))
            {
                requestBody.Add("client_secret", clientSecret);
            }

            var content = new FormUrlEncodedContent(requestBody);
            var response = await client.PostAsync(tokenEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Keycloak login failed: {Error}", errorContent);
                return Unauthorized(new { message = "Invalid username or password" });
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            return Ok(new
            {
                accessToken = tokenResponse?.AccessToken,
                refreshToken = tokenResponse?.RefreshToken,
                expiresIn = tokenResponse?.ExpiresIn,
                tokenType = tokenResponse?.TokenType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error");
            return StatusCode(500, new { message = "Authentication service unavailable" });
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
    {
        try
        {
            var keycloakUrl = _configuration["Keycloak:Authority"] ?? "http://localhost:8080/realms/hrm";
            var clientId = _configuration["Keycloak:ClientId"] ?? "hrm-frontend";
            var clientSecret = _configuration["Keycloak:ClientSecret"] ?? "";

            var tokenEndpoint = $"{keycloakUrl}/protocol/openid-connect/token";

            var client = _httpClientFactory.CreateClient();

            var requestBody = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "client_id", clientId },
                { "refresh_token", dto.RefreshToken }
            };

            if (!string.IsNullOrEmpty(clientSecret))
            {
                requestBody.Add("client_secret", clientSecret);
            }

            var content = new FormUrlEncodedContent(requestBody);
            var response = await client.PostAsync(tokenEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                return Unauthorized(new { message = "Token refresh failed" });
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            return Ok(new
            {
                accessToken = tokenResponse?.AccessToken,
                refreshToken = tokenResponse?.RefreshToken,
                expiresIn = tokenResponse?.ExpiresIn,
                tokenType = tokenResponse?.TokenType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh error");
            return StatusCode(500, new { message = "Token refresh failed" });
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] LogoutDto dto)
    {
        try
        {
            var keycloakUrl = _configuration["Keycloak:Authority"] ?? "http://localhost:8080/realms/hrm";
            var clientId = _configuration["Keycloak:ClientId"] ?? "hrm-frontend";
            var clientSecret = _configuration["Keycloak:ClientSecret"] ?? "";

            var logoutEndpoint = $"{keycloakUrl}/protocol/openid-connect/logout";

            var client = _httpClientFactory.CreateClient();

            var requestBody = new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "refresh_token", dto.RefreshToken }
            };

            if (!string.IsNullOrEmpty(clientSecret))
            {
                requestBody.Add("client_secret", clientSecret);
            }

            var content = new FormUrlEncodedContent(requestBody);
            await client.PostAsync(logoutEndpoint, content);

            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout error");
            return Ok(new { message = "Logged out" });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst("sub")?.Value;
        var username = User.FindFirst("preferred_username")?.Value;
        var email = User.FindFirst("email")?.Value;
        var firstName = User.FindFirst("given_name")?.Value;
        var lastName = User.FindFirst("family_name")?.Value;
        var roles = User.FindAll("realm_access")?.Select(c => c.Value).ToList() ?? new List<string>();

        return Ok(new
        {
            id = userId,
            username = username,
            email = email,
            firstName = firstName,
            lastName = lastName,
            roles = roles
        });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            var keycloakBaseUrl = _configuration["Keycloak:Authority"]?.Replace("/realms/hrm", "")
                ?? "http://localhost:8080";
            var realm = _configuration["Keycloak:Realm"] ?? "hrm";
            var adminUsername = _configuration["Keycloak:AdminUsername"] ?? "admin";
            var adminPassword = _configuration["Keycloak:AdminPassword"] ?? "admin";

            var client = _httpClientFactory.CreateClient();

            // Get admin token
            var tokenEndpoint = $"{keycloakBaseUrl}/realms/master/protocol/openid-connect/token";
            var tokenBody = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "client_id", "admin-cli" },
                { "username", adminUsername },
                { "password", adminPassword }
            };

            var tokenContent = new FormUrlEncodedContent(tokenBody);
            var tokenResponse = await client.PostAsync(tokenEndpoint, tokenContent);

            if (!tokenResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get admin token for password change");
                return StatusCode(500, new { message = "Password change service unavailable" });
            }

            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var adminToken = JsonSerializer.Deserialize<KeycloakTokenResponse>(tokenJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            // Change password via Keycloak Admin API
            var resetPasswordEndpoint = $"{keycloakBaseUrl}/admin/realms/{realm}/users/{userId}/reset-password";

            var passwordPayload = new
            {
                type = "password",
                value = dto.NewPassword,
                temporary = false
            };

            var request = new HttpRequestMessage(HttpMethod.Put, resetPasswordEndpoint)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(passwordPayload),
                    System.Text.Encoding.UTF8,
                    "application/json"
                )
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken?.AccessToken);

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Password change failed: {Error}", error);
                return BadRequest(new { message = "Failed to change password" });
            }

            return Ok(new { message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change password error");
            return StatusCode(500, new { message = "Password change failed" });
        }
    }
}

public class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RefreshTokenDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class KeycloakTokenResponse
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
    public string? TokenType { get; set; }
    public int RefreshExpiresIn { get; set; }
}
