using ApiGateway.Models;
using ApiGateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ApiGateway.Controllers;

/// <summary>
/// Overtime Management API - handles overtime requests and approvals
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OvertimeController : ControllerBase
{
    private readonly ITimeGrpcService _timeService;
    private readonly ILogger<OvertimeController> _logger;

    public OvertimeController(
        ITimeGrpcService timeService,
        ILogger<OvertimeController> logger)
    {
        _timeService = timeService;
        _logger = logger;
    }

    /// <summary>
    /// Create an overtime request
    /// </summary>
    [HttpPost("request")]
    public async Task<IActionResult> CreateOvertimeRequest([FromBody] CreateOvertimeRequestDto dto)
    {
        try
        {
            var employeeId = GetCurrentEmployeeId();
            var request = new Protos.CreateOvertimeRequestRequest
            {
                EmployeeId = dto.EmployeeId ?? employeeId,
                Date = dto.Date,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                TotalMinutes = dto.TotalMinutes,
                Reason = dto.Reason ?? ""
            };

            var response = await _timeService.CreateOvertimeRequestAsync(request);
            return CreatedAtAction(nameof(GetOvertimeRequest), new { id = response.Id }, MapToDto(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating overtime request");
            return StatusCode(500, new { message = "Failed to create overtime request" });
        }
    }

    /// <summary>
    /// Get overtime requests (with filters)
    /// </summary>
    [HttpGet("requests")]
    public async Task<IActionResult> GetOvertimeRequests(
        [FromQuery] string? employeeId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var id = employeeId ?? GetCurrentEmployeeId();
            var response = await _timeService.GetOvertimeRequestsAsync(id, status, startDate, endDate, page, pageSize);

            return Ok(new
            {
                data = response.Requests.Select(MapToDto),
                totalCount = response.TotalCount,
                page = response.Page,
                pageSize = response.PageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching overtime requests");
            return StatusCode(500, new { message = "Failed to fetch overtime requests" });
        }
    }

    /// <summary>
    /// Get pending overtime requests (for managers/HR to approve)
    /// </summary>
    [HttpGet("requests/pending")]
    [Authorize(Policy = "ManagerOrHR")]
    public async Task<IActionResult> GetPendingOvertimeRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var approverId = GetCurrentEmployeeId();
            var response = await _timeService.GetOvertimeRequestsAsync(null, "pending", null, null, page, pageSize);

            return Ok(new
            {
                data = response.Requests.Select(MapToDto),
                totalCount = response.TotalCount,
                page = response.Page,
                pageSize = response.PageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending overtime requests");
            return StatusCode(500, new { message = "Failed to fetch pending overtime requests" });
        }
    }

    /// <summary>
    /// Get specific overtime request
    /// </summary>
    [HttpGet("request/{id}")]
    public async Task<IActionResult> GetOvertimeRequest(string id)
    {
        try
        {
            var response = await _timeService.GetOvertimeRequestDetailAsync(id);
            if (string.IsNullOrEmpty(response.Id))
            {
                return NotFound(new { message = "Overtime request not found" });
            }
            return Ok(MapToDto(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching overtime request");
            return StatusCode(500, new { message = "Failed to fetch overtime request" });
        }
    }

    /// <summary>
    /// Approve overtime request
    /// </summary>
    [HttpPost("request/{id}/approve")]
    [Authorize(Policy = "ManagerOrHR")]
    public async Task<IActionResult> ApproveOvertimeRequest(string id, [FromBody] ApproveOvertimeRequestDto dto)
    {
        try
        {
            var approverId = GetCurrentEmployeeId();
            var response = await _timeService.ApproveOvertimeRequestAsync(id, approverId, dto.Comment);
            return Ok(MapToDto(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving overtime request");
            return StatusCode(500, new { message = "Failed to approve overtime request" });
        }
    }

    /// <summary>
    /// Reject overtime request
    /// </summary>
    [HttpPost("request/{id}/reject")]
    [Authorize(Policy = "ManagerOrHR")]
    public async Task<IActionResult> RejectOvertimeRequest(string id, [FromBody] RejectOvertimeRequestDto dto)
    {
        try
        {
            var approverId = GetCurrentEmployeeId();
            var response = await _timeService.RejectOvertimeRequestAsync(id, approverId, dto.Reason);
            return Ok(MapToDto(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting overtime request");
            return StatusCode(500, new { message = "Failed to reject overtime request" });
        }
    }

    private string GetCurrentEmployeeId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
    }

    private static object MapToDto(Protos.OvertimeRequestResponse r) => new
    {
        id = r.Id,
        employeeId = r.EmployeeId,
        employeeName = r.EmployeeName,
        date = r.Date,
        startTime = r.StartTime,
        endTime = r.EndTime,
        totalMinutes = r.TotalMinutes,
        reason = r.Reason,
        status = r.Status,
        approverId = r.ApproverId,
        approverName = r.ApproverName,
        approverComment = r.ApproverComment,
        approvedAt = r.ApprovedAt,
        createdAt = r.CreatedAt,
        updatedAt = r.UpdatedAt
    };
}
