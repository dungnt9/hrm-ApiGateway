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
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        var response = await _employeeService.GetEmployeesAsync(page, pageSize, departmentId, teamId, search, status);
        return Ok(new
        {
            data = response.Employees.Select(MapToDto),
            totalCount = response.TotalCount,
            page = response.Page,
            pageSize = response.PageSize
        });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentEmployee()
    {
        var keycloakId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(keycloakId))
            return Unauthorized();

        var response = await _employeeService.GetEmployeeByKeycloakIdAsync(keycloakId);

        // Fallback: seed data may store username instead of UUID — try preferred_username
        if (string.IsNullOrEmpty(response.Id))
        {
            var username = User.FindFirst("preferred_username")?.Value;
            if (!string.IsNullOrEmpty(username))
                response = await _employeeService.GetEmployeeByKeycloakIdAsync(username);
        }

        if (string.IsNullOrEmpty(response.Id))
            return NotFound(new { message = "Employee not found" });

        return Ok(MapToDto(response));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateCurrentEmployee([FromBody] UpdateEmployeeDto dto)
    {
        var keycloakId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(keycloakId))
            return Unauthorized();

        var existing = await _employeeService.GetEmployeeByKeycloakIdAsync(keycloakId);
        if (string.IsNullOrEmpty(existing.Id))
            return NotFound(new { message = "Employee not found" });

        var request = new Protos.UpdateEmployeeRequest
        {
            EmployeeId = existing.Id,
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
        if (string.IsNullOrEmpty(response.Id))
            return NotFound(new { message = "Employee not found" });

        return Ok(MapToDto(response));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEmployee(string id)
    {
        var response = await _employeeService.GetEmployeeAsync(id);
        if (string.IsNullOrEmpty(response.Id))
            return NotFound(new { message = "Employee not found" });

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
        if (string.IsNullOrEmpty(response.Id))
            return NotFound(new { message = "Employee not found" });

        return Ok(MapToDto(response));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> DeleteEmployee(string id)
    {
        var response = await _employeeService.DeleteEmployeeAsync(id);
        if (!response.Success)
            return BadRequest(new { message = response.Message });

        return NoContent();
    }

    [HttpGet("{id}/manager")]
    public async Task<IActionResult> GetEmployeeManager(string id)
    {
        var response = await _employeeService.GetEmployeeManagerAsync(id);
        if (string.IsNullOrEmpty(response.Id))
            return NotFound(new { message = "Manager not found" });

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
            return BadRequest(new { message = response.Message });

        return Ok(new { message = response.Message });
    }

    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments([FromQuery] string? companyId = null)
    {
        var response = await _employeeService.GetDepartmentsAsync(companyId);
        return Ok(response.Departments.Select(MapDepartmentToDto));
    }

    [HttpGet("departments/{id}")]
    public async Task<IActionResult> GetDepartment(string id)
    {
        var d = await _employeeService.GetDepartmentAsync(id);
        if (string.IsNullOrEmpty(d.Id))
            return NotFound(new { message = "Department not found" });
        return Ok(MapDepartmentToDto(d));
    }

    [HttpPost("departments")]
    [Authorize(Policy = "HRStaff")]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentDto dto)
    {
        var request = new Protos.CreateDepartmentRequest
        {
            Name = dto.Name,
            Description = dto.Description ?? "",
            ManagerId = dto.ManagerId ?? ""
        };
        var d = await _employeeService.CreateDepartmentAsync(request);
        return CreatedAtAction(nameof(GetDepartment), new { id = d.Id }, MapDepartmentToDto(d));
    }

    [HttpPut("departments/{id}")]
    [Authorize(Policy = "HRStaff")]
    public async Task<IActionResult> UpdateDepartment(string id, [FromBody] UpdateDepartmentDto dto)
    {
        var request = new Protos.UpdateDepartmentRequest
        {
            DepartmentId = id,
            Name = dto.Name,
            Description = dto.Description ?? "",
            ManagerId = dto.ManagerId ?? ""
        };
        var d = await _employeeService.UpdateDepartmentAsync(request);
        if (string.IsNullOrEmpty(d.Id))
            return NotFound(new { message = "Department not found" });
        return Ok(MapDepartmentToDto(d));
    }

    [HttpDelete("departments/{id}")]
    [Authorize(Policy = "HRStaff")]
    public async Task<IActionResult> DeleteDepartment(string id)
    {
        var response = await _employeeService.DeleteDepartmentAsync(id);
        if (!response.Success)
            return BadRequest(new { message = response.Message });
        return NoContent();
    }

    [HttpGet("teams")]
    public async Task<IActionResult> GetTeams([FromQuery] string? departmentId = null)
    {
        var response = await _employeeService.GetTeamsAsync(departmentId);
        return Ok(response.Teams.Select(MapTeamToDto));
    }

    [HttpGet("teams/{id}")]
    public async Task<IActionResult> GetTeam(string id)
    {
        var t = await _employeeService.GetTeamAsync(id);
        if (string.IsNullOrEmpty(t.Id))
            return NotFound(new { message = "Team not found" });
        return Ok(MapTeamToDto(t));
    }

    [HttpPost("teams")]
    [Authorize(Policy = "HRStaff")]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeamDto dto)
    {
        var request = new Protos.CreateTeamRequest
        {
            Name = dto.Name,
            Description = dto.Description ?? "",
            DepartmentId = dto.DepartmentId,
            LeaderId = dto.LeaderId ?? ""
        };
        var t = await _employeeService.CreateTeamAsync(request);
        return CreatedAtAction(nameof(GetTeam), new { id = t.Id }, MapTeamToDto(t));
    }

    [HttpPut("teams/{id}")]
    [Authorize(Policy = "HRStaff")]
    public async Task<IActionResult> UpdateTeam(string id, [FromBody] UpdateTeamDto dto)
    {
        var request = new Protos.UpdateTeamRequest
        {
            TeamId = id,
            Name = dto.Name,
            Description = dto.Description ?? "",
            DepartmentId = dto.DepartmentId ?? "",
            LeaderId = dto.LeaderId ?? ""
        };
        var t = await _employeeService.UpdateTeamAsync(request);
        if (string.IsNullOrEmpty(t.Id))
            return NotFound(new { message = "Team not found" });
        return Ok(MapTeamToDto(t));
    }

    [HttpDelete("teams/{id}")]
    [Authorize(Policy = "HRStaff")]
    public async Task<IActionResult> DeleteTeam(string id)
    {
        var response = await _employeeService.DeleteTeamAsync(id);
        if (!response.Success)
            return BadRequest(new { message = response.Message });
        return NoContent();
    }

    private static object MapDepartmentToDto(Protos.Department d) => new
    {
        id = d.Id,
        name = d.Name,
        companyId = d.CompanyId,
        managerId = d.ManagerId,
        managerName = d.ManagerName,
        description = d.Description,
        createdAt = d.CreatedAt
    };

    private static object MapTeamToDto(Protos.Team t) => new
    {
        id = t.Id,
        name = t.Name,
        departmentId = t.DepartmentId,
        managerId = t.ManagerId,
        managerName = t.ManagerName,
        description = t.Description,
        createdAt = t.CreatedAt
    };

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
