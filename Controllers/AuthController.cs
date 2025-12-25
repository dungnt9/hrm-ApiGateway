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

public class KeycloakTokenResponse
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
    public string? TokenType { get; set; }
    public int RefreshExpiresIn { get; set; }
}
