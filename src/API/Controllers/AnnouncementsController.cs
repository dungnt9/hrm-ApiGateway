using ApiGateway.Models;
using ApiGateway.Protos;
using ApiGateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnnouncementsController : ControllerBase
{
    private readonly IEmployeeGrpcService _employeeService;
    private readonly ILogger<AnnouncementsController> _logger;

    public AnnouncementsController(IEmployeeGrpcService employeeService, ILogger<AnnouncementsController> logger)
    {
        _employeeService = employeeService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAnnouncements(
        [FromQuery] string? category = null,
        [FromQuery] string? departmentId = null,
        [FromQuery] bool includeExpired = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var request = new GetAnnouncementsRequest
        {
            Category = category ?? "",
            DepartmentId = departmentId ?? "",
            IncludeExpired = includeExpired,
            Page = page,
            PageSize = pageSize
        };

        var response = await _employeeService.GetAnnouncementsAsync(request);
        return Ok(new
        {
            data = response.Announcements.Select(MapToDto),
            totalCount = response.TotalCount
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAnnouncement(string id)
    {
        var response = await _employeeService.GetAnnouncementAsync(id);
        if (string.IsNullOrEmpty(response.Id))
            return NotFound(new { message = "Announcement not found" });

        return Ok(MapToDto(response));
    }

    [HttpPost]
    [Authorize(Policy = "HRStaff")]
    public async Task<IActionResult> CreateAnnouncement([FromBody] CreateAnnouncementDto dto)
    {
        var keycloakId = User.FindFirst("sub")?.Value ?? "";
        var creator = await _employeeService.GetEmployeeByKeycloakIdAsync(keycloakId);

        if (string.IsNullOrEmpty(creator.Id))
        {
            var username = User.FindFirst("preferred_username")?.Value;
            if (!string.IsNullOrEmpty(username))
                creator = await _employeeService.GetEmployeeByKeycloakIdAsync(username);
        }

        if (string.IsNullOrEmpty(creator.Id))
            return Unauthorized(new { message = "Creator not found" });

        var request = new CreateAnnouncementRequest
        {
            Title = dto.Title,
            Content = dto.Content,
            Category = dto.Category,
            IsPinned = dto.IsPinned,
            ExpiresAt = dto.ExpiresAt ?? "",
            DepartmentId = dto.DepartmentId ?? "",
            CreatedBy = creator.Id
        };

        var response = await _employeeService.CreateAnnouncementAsync(request);
        return CreatedAtAction(nameof(GetAnnouncement), new { id = response.Id }, MapToDto(response));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "HRStaff")]
    public async Task<IActionResult> UpdateAnnouncement(string id, [FromBody] UpdateAnnouncementDto dto)
    {
        var request = new UpdateAnnouncementRequest
        {
            Id = id,
            Title = dto.Title,
            Content = dto.Content,
            Category = dto.Category,
            IsPinned = dto.IsPinned,
            ExpiresAt = dto.ExpiresAt ?? "",
            DepartmentId = dto.DepartmentId ?? ""
        };

        var response = await _employeeService.UpdateAnnouncementAsync(request);
        if (string.IsNullOrEmpty(response.Id))
            return NotFound(new { message = "Announcement not found" });

        return Ok(MapToDto(response));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "HRStaff")]
    public async Task<IActionResult> DeleteAnnouncement(string id)
    {
        var response = await _employeeService.DeleteAnnouncementAsync(id);
        if (!response.Success)
            return BadRequest(new { message = response.Message });

        return NoContent();
    }

    private static object MapToDto(AnnouncementResponse a) => new
    {
        id = a.Id,
        title = a.Title,
        content = a.Content,
        category = a.Category,
        isPinned = a.IsPinned,
        expiresAt = string.IsNullOrEmpty(a.ExpiresAt) ? (string?)null : a.ExpiresAt,
        departmentId = string.IsNullOrEmpty(a.DepartmentId) ? (string?)null : a.DepartmentId,
        departmentName = string.IsNullOrEmpty(a.DepartmentName) ? (string?)null : a.DepartmentName,
        createdBy = a.CreatedBy,
        createdByName = string.IsNullOrEmpty(a.CreatedByName) ? (string?)null : a.CreatedByName,
        createdAt = a.CreatedAt,
        updatedAt = a.UpdatedAt
    };
}
