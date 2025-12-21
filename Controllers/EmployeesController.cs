using ApiGateway.Models;
using ApiGateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeGrpcService _employeeService;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(IEmployeeGrpcService employeeService, ILogger<EmployeesController> logger)
    {
        _employeeService = employeeService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? departmentId = null,
        [FromQuery] string? teamId = null,
        [FromQuery] string? search = null)
    {
        var response = await _employeeService.GetEmployeesAsync(page, pageSize, departmentId, teamId, search);
        return Ok(new
        {
            data = response.Employees.Select(MapToDto),
            totalCount = response.TotalCount,
            page = response.Page,
            pageSize = response.PageSize
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEmployee(string id)
    {
        var response = await _employeeService.GetEmployeeAsync(id);
        if (string.IsNullOrEmpty(response.Id))
        {
            return NotFound(new { message = "Employee not found" });
        }
        return Ok(MapToDto(response));
    }

    [HttpPost]
    [Authorize(Policy = "HRStaff")]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
    {
        var request = new Protos.CreateEmployeeRequest
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Phone = dto.Phone ?? "",
            DepartmentId = dto.DepartmentId ?? "",
            TeamId = dto.TeamId ?? "",
            Position = dto.Position ?? "",
            ManagerId = dto.ManagerId ?? "",
            KeycloakUserId = dto.KeycloakUserId ?? "",
            HireDate = dto.HireDate ?? ""
        };

        var response = await _employeeService.CreateEmployeeAsync(request);
        return CreatedAtAction(nameof(GetEmployee), new { id = response.Id }, MapToDto(response));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "HRStaff")]
    public async Task<IActionResult> UpdateEmployee(string id, [FromBody] UpdateEmployeeDto dto)
    {
        var request = new Protos.UpdateEmployeeRequest
        {
            EmployeeId = id,
            FirstName = dto.FirstName ?? "",
            LastName = dto.LastName ?? "",
            Email = dto.Email ?? "",
            Phone = dto.Phone ?? "",
            DepartmentId = dto.DepartmentId ?? "",
            TeamId = dto.TeamId ?? "",
            Position = dto.Position ?? "",
            ManagerId = dto.ManagerId ?? "",
            Status = dto.Status ?? ""
        };

        var response = await _employeeService.UpdateEmployeeAsync(request);
        return Ok(MapToDto(response));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> DeleteEmployee(string id)
    {
        var response = await _employeeService.DeleteEmployeeAsync(id);
        if (!response.Success)
        {
            return BadRequest(new { message = response.Message });
        }
        return NoContent();
    }

    [HttpGet("{id}/manager")]
    public async Task<IActionResult> GetEmployeeManager(string id)
    {
        var response = await _employeeService.GetEmployeeManagerAsync(id);
        if (string.IsNullOrEmpty(response.Id))
        {
            return NotFound(new { message = "Manager not found" });
        }
        return Ok(MapToDto(response));
    }

    [HttpGet("team/{teamId}")]
    [Authorize(Policy = "ManagerOrHR")]
    public async Task<IActionResult> GetTeamMembers(string teamId)
    {
        var response = await _employeeService.GetTeamMembersAsync(teamId, null);
        return Ok(response.Employees.Select(MapToDto));
    }

    [HttpGet("manager/{managerId}/team")]
    [Authorize(Policy = "ManagerOrHR")]
    public async Task<IActionResult> GetManagerTeamMembers(string managerId)
    {
        var response = await _employeeService.GetTeamMembersAsync(null, managerId);
        return Ok(response.Employees.Select(MapToDto));
    }

    [HttpPost("{id}/assign-role")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> AssignRole(string id, [FromBody] AssignRoleDto dto)
    {
        var response = await _employeeService.AssignRoleAsync(id, dto.Role);
        if (!response.Success)
        {
            return BadRequest(new { message = response.Message });
        }
        return Ok(new { message = response.Message });
    }

    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments([FromQuery] string? companyId = null)
    {
        var response = await _employeeService.GetDepartmentsAsync(companyId);
        return Ok(response.Departments.Select(d => new
        {
            id = d.Id,
            name = d.Name,
            companyId = d.CompanyId,
            managerId = d.ManagerId,
            managerName = d.ManagerName,
            createdAt = d.CreatedAt
        }));
    }

    [HttpGet("teams")]
    public async Task<IActionResult> GetTeams([FromQuery] string? departmentId = null)
    {
        var response = await _employeeService.GetTeamsAsync(departmentId);
        return Ok(response.Teams.Select(t => new
        {
            id = t.Id,
            name = t.Name,
            departmentId = t.DepartmentId,
            managerId = t.ManagerId,
            managerName = t.ManagerName,
            createdAt = t.CreatedAt
        }));
    }

    private static object MapToDto(Protos.EmployeeResponse e) => new
    {
        id = e.Id,
        firstName = e.FirstName,
        lastName = e.LastName,
        email = e.Email,
        phone = e.Phone,
        departmentId = e.DepartmentId,
        departmentName = e.DepartmentName,
        teamId = e.TeamId,
        teamName = e.TeamName,
        position = e.Position,
        managerId = e.ManagerId,
        managerName = e.ManagerName,
        keycloakUserId = e.KeycloakUserId,
        hireDate = e.HireDate,
        status = e.Status,
        createdAt = e.CreatedAt,
        updatedAt = e.UpdatedAt
    };
}
