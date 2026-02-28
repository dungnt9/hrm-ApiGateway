using ApiGateway.Models;
using ApiGateway.Services;
using ClosedXML.Excel;
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
        var rawId = dto.EmployeeId ?? GetCurrentEmployeeId();
        var employeeId = await ResolveEmployeeIdAsync(rawId);
        var request = new Protos.CheckInRequest
        {
            EmployeeId = employeeId,
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
        var rawId = dto.EmployeeId ?? GetCurrentEmployeeId();
        var employeeId = await ResolveEmployeeIdAsync(rawId);
        var request = new Protos.CheckOutRequest
        {
            EmployeeId = employeeId,
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
        var rawId = employeeId ?? GetCurrentEmployeeId();
        var id = await ResolveEmployeeIdAsync(rawId);
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
        var rawId = employeeId ?? GetCurrentEmployeeId();
        var id = await ResolveEmployeeIdAsync(rawId);
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
    /// <summary>
    /// Export attendance history to Excel with modern formatting
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> ExportAttendanceToExcel(
        [FromQuery] string? employeeId = null,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null)
    {
        try
        {
            var rawId = employeeId ?? GetCurrentEmployeeId();
            var id = await ResolveEmployeeIdAsync(rawId);
            
            // Get employee info
            var employee = await _employeeService.GetEmployeeAsync(id);
            var employeeName = $"{employee.FirstName} {employee.LastName}";
            var employeeCode = employee.EmployeeCode;

            // Get attendance data
            var response = await _timeService.GetAttendanceHistoryAsync(id, startDate, endDate, 1, 1000);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Attendance Report");

            // Title styling
            worksheet.Cell("A1").Value = "ATTENDANCE REPORT";
            worksheet.Cell("A1").Style.Font.FontSize = 16;
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range("A1:H1").Merge();

            // Employee info section
            worksheet.Cell("A3").Value = "Employee:";
            worksheet.Cell("B3").Value = employeeName;
            worksheet.Cell("A4").Value = "Employee Code:";
            worksheet.Cell("B4").Value = employeeCode;
            worksheet.Cell("A5").Value = "Period:";
            worksheet.Cell("B5").Value = $"{startDate ?? "N/A"} to {endDate ?? "N/A"}";
            worksheet.Cell("A6").Value = "Generated:";
            worksheet.Cell("B6").Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            worksheet.Range("A3:A6").Style.Font.Bold = true;

            // Headers
            int headerRow = 8;
            var headers = new[] { "Date", "Check In", "Check Out", "Total Hours", "Status", "Late (min)", "Early Leave (min)", "Note" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(79, 129, 189);
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // Data rows
            int row = headerRow + 1;
            foreach (var record in response.Records)
            {
                worksheet.Cell(row, 1).Value = record.Date;
                worksheet.Cell(row, 2).Value = record.CheckInTime;
                worksheet.Cell(row, 3).Value = record.CheckOutTime;
                worksheet.Cell(row, 4).Value = record.TotalHours;
                worksheet.Cell(row, 5).Value = record.CheckInStatus;
                worksheet.Cell(row, 6).Value = record.LateMinutes;
                worksheet.Cell(row, 7).Value = record.EarlyLeaveMinutes;
                worksheet.Cell(row, 8).Value = record.Note;

                // Apply zebra striping
                if ((row - headerRow) % 2 == 0)
                {
                    worksheet.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.FromArgb(220, 230, 241);
                }

                // Color code status
                var statusCell = worksheet.Cell(row, 5);
                switch (record.CheckInStatus?.ToLower())
                {
                    case "on_time":
                    case "ontime":
                        statusCell.Style.Font.FontColor = XLColor.Green;
                        break;
                    case "late":
                        statusCell.Style.Font.FontColor = XLColor.Orange;
                        break;
                    case "absent":
                        statusCell.Style.Font.FontColor = XLColor.Red;
                        break;
                }

                // Apply borders
                worksheet.Range(row, 1, row, 8).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                row++;
            }

            // Summary section
            if (response.Summary != null)
            {
                row += 2;
                worksheet.Cell(row, 1).Value = "SUMMARY";
                worksheet.Cell(row, 1).Style.Font.Bold = true;
                worksheet.Cell(row, 1).Style.Font.FontSize = 12;
                row++;

                worksheet.Cell(row, 1).Value = "Total Days:";
                worksheet.Cell(row, 2).Value = response.Summary.TotalDays;
                row++;
                worksheet.Cell(row, 1).Value = "Present Days:";
                worksheet.Cell(row, 2).Value = response.Summary.PresentDays;
                row++;
                worksheet.Cell(row, 1).Value = "Absent Days:";
                worksheet.Cell(row, 2).Value = response.Summary.AbsentDays;
                row++;
                worksheet.Cell(row, 1).Value = "Late Count:";
                worksheet.Cell(row, 2).Value = response.Summary.LateCount;
                row++;
                worksheet.Cell(row, 1).Value = "Total Hours:";
                worksheet.Cell(row, 2).Value = response.Summary.TotalHours;

                worksheet.Range(row - 5, 1, row, 1).Style.Font.Bold = true;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Create memory stream
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"Attendance_{employeeCode}_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting attendance to Excel");
            return StatusCode(500, new { message = "Failed to export attendance data" });
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
        var rawId = employeeId ?? GetCurrentEmployeeId();
        var id = await ResolveEmployeeIdAsync(rawId);
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
        // Get employee_id from JWT claims — only use if it's a valid GUID (DB employee ID)
        var employeeId = User.FindFirst("employee_id")?.Value;
        if (!string.IsNullOrEmpty(employeeId) && Guid.TryParse(employeeId, out _))
            return employeeId;

        // Use sub (Keycloak user UUID) — ResolveEmployeeIdAsync will resolve to DB ID
        return User.FindFirst("sub")?.Value ?? "";
    }

    private async Task<string> ResolveEmployeeIdAsync(string rawId)
    {
        // Try Keycloak lookup first — resolves both Keycloak UUIDs and non-GUID identifiers to DB ID
        try
        {
            var employee = await _employeeService.GetEmployeeByKeycloakIdAsync(rawId);
            if (!string.IsNullOrEmpty(employee.Id))
                return employee.Id;

            // Fallback: seed data may store username instead of UUID — try preferred_username
            var username = User.FindFirst("preferred_username")?.Value;
            if (!string.IsNullOrEmpty(username))
            {
                employee = await _employeeService.GetEmployeeByKeycloakIdAsync(username);
                if (!string.IsNullOrEmpty(employee.Id))
                    return employee.Id;
            }
        }
        catch { }

        // rawId is already a DB employee ID (GUID)
        return rawId;
    }
}
