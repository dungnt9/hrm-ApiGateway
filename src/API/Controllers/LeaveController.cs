using ApiGateway.Models;
using ApiGateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveController : ControllerBase
{
    private readonly ITimeGrpcService _timeService;
    private readonly ILogger<LeaveController> _logger;

    public LeaveController(ITimeGrpcService timeService, ILogger<LeaveController> logger)
    {
        _timeService = timeService;
        _logger = logger;
    }

    [HttpPost("request")]
    public async Task<IActionResult> CreateLeaveRequest([FromBody] CreateLeaveRequestDto dto)
    {
        var employeeId = GetCurrentEmployeeId();
        var request = new Protos.CreateLeaveRequestRequest
        {
            EmployeeId = dto.EmployeeId ?? employeeId,
            LeaveType = dto.LeaveType,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Reason = dto.Reason ?? "",
            ApproverId = dto.ApproverId,
            ApproverType = dto.ApproverType
        };

        var response = await _timeService.CreateLeaveRequestAsync(request);
        return CreatedAtAction(nameof(GetLeaveRequest), new { id = response.Id }, MapToDto(response));
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetLeaveRequests(
        [FromQuery] string? employeeId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? leaveType = null,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var id = employeeId ?? GetCurrentEmployeeId();
        var response = await _timeService.GetLeaveRequestsAsync(id, null, status, leaveType, startDate, endDate, page, pageSize);
        
        return Ok(new
        {
            data = response.Requests.Select(MapToDto),
            totalCount = response.TotalCount,
            page = response.Page,
            pageSize = response.PageSize
        });
    }

    [HttpGet("requests/pending")]
    [Authorize(Policy = "ManagerOrHR")]
    public async Task<IActionResult> GetPendingLeaveRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var approverId = GetCurrentEmployeeId();
        var response = await _timeService.GetLeaveRequestsAsync(null, approverId, "pending", null, null, null, page, pageSize);
        
        return Ok(new
        {
            data = response.Requests.Select(MapToDto),
            totalCount = response.TotalCount,
            page = response.Page,
            pageSize = response.PageSize
        });
    }

    [HttpGet("request/{id}")]
    public async Task<IActionResult> GetLeaveRequest(string id)
    {
        var response = await _timeService.GetLeaveRequestDetailAsync(id);
        if (string.IsNullOrEmpty(response.Id))
        {
            return NotFound(new { message = "Leave request not found" });
        }
        return Ok(MapToDto(response));
    }

    [HttpPost("request/{id}/approve")]
    [Authorize(Policy = "ManagerOrHR")]
    public async Task<IActionResult> ApproveLeaveRequest(string id, [FromBody] ApproveLeaveRequestDto dto)
    {
        var approverId = GetCurrentEmployeeId();
        var response = await _timeService.ApproveLeaveRequestAsync(id, approverId, dto.Note);
        return Ok(MapToDto(response));
    }

    [HttpPost("request/{id}/reject")]
    [Authorize(Policy = "ManagerOrHR")]
    public async Task<IActionResult> RejectLeaveRequest(string id, [FromBody] RejectLeaveRequestDto dto)
    {
        var approverId = GetCurrentEmployeeId();
        var response = await _timeService.RejectLeaveRequestAsync(id, approverId, dto.Reason);
        return Ok(MapToDto(response));
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetLeaveBalance([FromQuery] string? employeeId = null, [FromQuery] int? year = null)
    {
        var id = employeeId ?? GetCurrentEmployeeId();
        var targetYear = year ?? DateTime.UtcNow.Year;
        var response = await _timeService.GetLeaveBalanceAsync(id, targetYear);
        
        return Ok(new
        {
            employeeId = response.EmployeeId,
            year = response.Year,
            annual = new
            {
                total = response.AnnualTotal,
                used = response.AnnualUsed,
                remaining = response.AnnualRemaining
            },
            sick = new
            {
                total = response.SickTotal,
                used = response.SickUsed,
                remaining = response.SickRemaining
            },
            unpaidUsed = response.UnpaidUsed
        });
    }

    private string GetCurrentEmployeeId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
    }

    private static object MapToDto(Protos.LeaveRequestResponse r) => new
    {
        id = r.Id,
        employeeId = r.EmployeeId,
        employeeName = r.EmployeeName,
        leaveType = r.LeaveType,
        startDate = r.StartDate,
        endDate = r.EndDate,
        totalDays = r.TotalDays,
        reason = r.Reason,
        status = r.Status,
        approverId = r.ApproverId,
        approverName = r.ApproverName,
        approverType = r.ApproverType,
        approvedAt = r.ApprovedAt,
        rejectionReason = r.RejectionReason,
        createdAt = r.CreatedAt,
        updatedAt = r.UpdatedAt
    };
}
