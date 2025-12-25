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
    private readonly IEmployeeGrpcService _employeeService;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(
        ITimeGrpcService timeService,
        IEmployeeGrpcService employeeService,
        ILogger<AttendanceController> logger)
    {
        _timeService = timeService;
        _employeeService = employeeService;
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

    /// <summary>
    /// Get attendance for all team members on a specific date
    /// </summary>
    [HttpGet("team/{teamId}")]
    [Authorize(Policy = "ManagerOrHR")]
    public async Task<IActionResult> GetTeamAttendance(
        string teamId,
        [FromQuery] string? date = null)
    {
        try
        {
            var targetDate = date ?? DateTime.UtcNow.ToString("yyyy-MM-dd");

            // Get team members from employee service
            var employeesResponse = await _employeeService.GetTeamMembersAsync(teamId, null);
            if (employeesResponse.Employees == null || !employeesResponse.Employees.Any())
            {
                return Ok(new
                {
                    teamId = teamId,
                    date = targetDate,
                    members = Array.Empty<object>(),
                    summary = new
                    {
                        totalMembers = 0,
                        presentCount = 0,
                        absentCount = 0,
                        lateCount = 0,
                        presenceRate = 0.0m
                    }
                });
            }

            var attendanceRecords = new List<object>();
            int presentCount = 0, absentCount = 0, lateCount = 0;

            // Get attendance for each team member
            foreach (var employee in employeesResponse.Employees)
            {
                try
                {
                    var attendanceResponse = await _timeService.GetAttendanceStatusAsync(employee.Id, targetDate);

                    bool isPresent = attendanceResponse.IsCheckedIn;
                    if (isPresent) presentCount++;
                    else absentCount++;

                    // For more details, we could also fetch history
                    var historyResponse = await _timeService.GetAttendanceHistoryAsync(
                        employee.Id,
                        targetDate,
                        targetDate,
                        1,
                        1);

                    var record = historyResponse.Records.FirstOrDefault();

                    attendanceRecords.Add(new
                    {
                        employeeId = employee.Id,
                        employeeName = $"{employee.FirstName} {employee.LastName}",
                        position = employee.Position,
                        status = isPresent ? "Present" : "Absent",
                        checkInTime = record?.CheckInTime,
                        checkOutTime = record?.CheckOutTime,
                        totalHours = record?.TotalHours ?? 0,
                        lateMinutes = record?.LateMinutes ?? 0,
                        isLate = (record?.LateMinutes ?? 0) > 0
                    });

                    if ((record?.LateMinutes ?? 0) > 0)
                        lateCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Error fetching attendance for employee {employee.Id}: {ex.Message}");
                    // Continue with next employee on error
                    attendanceRecords.Add(new
                    {
                        employeeId = employee.Id,
                        employeeName = $"{employee.FirstName} {employee.LastName}",
                        position = employee.Position,
                        status = "Unknown",
                        error = "Failed to fetch attendance"
                    });
                    absentCount++;
                }
            }

            var totalMembers = employeesResponse.Employees.Count();
            var presenceRate = totalMembers > 0 ? (decimal)presentCount / totalMembers * 100 : 0;

            return Ok(new
            {
                teamId = teamId,
                date = targetDate,
                members = attendanceRecords.OrderByDescending(x => x),
                summary = new
                {
                    totalMembers = totalMembers,
                    presentCount = presentCount,
                    absentCount = absentCount,
                    lateCount = lateCount,
                    presenceRate = Math.Round(presenceRate, 2)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching team attendance");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
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
