using ApiGateway.Models;
using ApiGateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly ITimeGrpcService _timeService;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(ITimeGrpcService timeService, ILogger<AttendanceController> logger)
    {
        _timeService = timeService;
        _logger = logger;
    }

    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInDto dto)
    {
        var employeeId = GetCurrentEmployeeId();
        var request = new Protos.CheckInRequest
        {
            EmployeeId = dto.EmployeeId ?? employeeId,
            Note = dto.Note ?? "",
            Latitude = dto.Latitude ?? 0,
            Longitude = dto.Longitude ?? 0
        };

        var response = await _timeService.CheckInAsync(request);
        return Ok(new
        {
            id = response.Id,
            employeeId = response.EmployeeId,
            checkInTime = response.CheckInTime,
            status = response.Status,
            lateMinutes = response.LateMinutes,
            message = response.Message
        });
    }

    [HttpPost("check-out")]
    public async Task<IActionResult> CheckOut([FromBody] CheckOutDto dto)
    {
        var employeeId = GetCurrentEmployeeId();
        var request = new Protos.CheckOutRequest
        {
            EmployeeId = dto.EmployeeId ?? employeeId,
            Note = dto.Note ?? "",
            Latitude = dto.Latitude ?? 0,
            Longitude = dto.Longitude ?? 0
        };

        var response = await _timeService.CheckOutAsync(request);
        return Ok(new
        {
            id = response.Id,
            employeeId = response.EmployeeId,
            checkInTime = response.CheckInTime,
            checkOutTime = response.CheckOutTime,
            totalHours = response.TotalHours,
            status = response.Status,
            earlyLeaveMinutes = response.EarlyLeaveMinutes,
            overtimeMinutes = response.OvertimeMinutes,
            message = response.Message
        });
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetAttendanceStatus([FromQuery] string? employeeId = null, [FromQuery] string? date = null)
    {
        var id = employeeId ?? GetCurrentEmployeeId();
        var response = await _timeService.GetAttendanceStatusAsync(id, date);
        return Ok(new
        {
            isCheckedIn = response.IsCheckedIn,
            isCheckedOut = response.IsCheckedOut,
            checkInTime = response.CheckInTime,
            checkOutTime = response.CheckOutTime,
            currentHours = response.CurrentHours
        });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetAttendanceHistory(
        [FromQuery] string? employeeId = null,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var id = employeeId ?? GetCurrentEmployeeId();
        var response = await _timeService.GetAttendanceHistoryAsync(id, startDate, endDate, page, pageSize);
        
        return Ok(new
        {
            data = response.Records.Select(r => new
            {
                id = r.Id,
                employeeId = r.EmployeeId,
                date = r.Date,
                checkInTime = r.CheckInTime,
                checkOutTime = r.CheckOutTime,
                totalHours = r.TotalHours,
                checkInStatus = r.CheckInStatus,
                checkOutStatus = r.CheckOutStatus,
                lateMinutes = r.LateMinutes,
                earlyLeaveMinutes = r.EarlyLeaveMinutes,
                overtimeMinutes = r.OvertimeMinutes,
                note = r.Note
            }),
            totalCount = response.TotalCount,
            page = response.Page,
            pageSize = response.PageSize,
            summary = response.Summary != null ? new
            {
                totalDays = response.Summary.TotalDays,
                presentDays = response.Summary.PresentDays,
                absentDays = response.Summary.AbsentDays,
                lateCount = response.Summary.LateCount,
                earlyLeaveCount = response.Summary.EarlyLeaveCount,
                totalHours = response.Summary.TotalHours,
                averageHoursPerDay = response.Summary.AverageHoursPerDay
            } : null
        });
    }

    [HttpGet("team/{teamId}")]
    [Authorize(Policy = "ManagerOrHR")]
    public async Task<IActionResult> GetTeamAttendance(
        string teamId,
        [FromQuery] string? date = null)
    {
        // This would need to be implemented by calling employee service first
        // Then getting attendance for each team member
        return Ok(new { message = "Team attendance endpoint" });
    }

    [HttpGet("shifts")]
    public async Task<IActionResult> GetShifts([FromQuery] string? departmentId = null)
    {
        var response = await _timeService.GetShiftsAsync(departmentId);
        return Ok(response.Shifts.Select(s => new
        {
            id = s.Id,
            name = s.Name,
            startTime = s.StartTime,
            endTime = s.EndTime,
            breakMinutes = s.BreakMinutes,
            isDefault = s.IsDefault
        }));
    }

    [HttpGet("shift")]
    public async Task<IActionResult> GetEmployeeShift([FromQuery] string? employeeId = null, [FromQuery] string? date = null)
    {
        var id = employeeId ?? GetCurrentEmployeeId();
        var response = await _timeService.GetEmployeeShiftAsync(id, date);
        if (response.Shift == null)
        {
            return NotFound(new { message = "Shift not found" });
        }
        return Ok(new
        {
            id = response.Shift.Id,
            name = response.Shift.Name,
            startTime = response.Shift.StartTime,
            endTime = response.Shift.EndTime,
            breakMinutes = response.Shift.BreakMinutes,
            isDefault = response.Shift.IsDefault
        });
    }

    private string GetCurrentEmployeeId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
    }
}
