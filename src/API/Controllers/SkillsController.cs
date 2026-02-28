using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/employees/{employeeId}/skills")]
[Authorize]
public class SkillsController : ControllerBase
{
    private readonly ILogger<SkillsController> _logger;
    private readonly IConfiguration _configuration;

    public SkillsController(ILogger<SkillsController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Get all skills for an employee (Mock data for now)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetEmployeeSkills(string employeeId)
    {
        try
        {
            var currentUserKeycloakId = User.FindFirst("sub")?.Value;
            var roles = User.Claims.Where(c => c.Type == "roles").Select(c => c.Value).ToList();
            
            // Mock data - will be replaced with actual database query
            var skills = new List<object>
            {
                new
                {
                    id = Guid.NewGuid().ToString(),
                    employeeId = employeeId,
                    skillName = "C# Programming",
                    category = "Technical",
                    proficiencyLevel = 4,
                    lastAssessedDate = DateTime.UtcNow.AddMonths(-2),
                    assessedBy = "Manager",
                    notes = "Advanced ASP.NET Core development",
                    createdAt = DateTime.UtcNow.AddYears(-1)
                },
                new
                {
                    id = Guid.NewGuid().ToString(),
                    employeeId = employeeId,
                    skillName = "Leadership",
                    category = "Soft Skills",
                    proficiencyLevel = 3,
                    lastAssessedDate = DateTime.UtcNow.AddMonths(-1),
                    assessedBy = "HR",
                    notes = "Team leadership and mentoring",
                    createdAt = DateTime.UtcNow.AddMonths(-6)
                }
            };

            return Ok(skills);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching skills for employee {EmployeeId}", employeeId);
            return StatusCode(500, new { message = "Failed to fetch skills" });
        }
    }

    /// <summary>
    /// Add a new skill for an employee
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "ManagerOrHR")]
    public async Task<IActionResult> AddEmployeeSkill(string employeeId, [FromBody] AddSkillDto dto)
    {
        try
        {
            var newSkill = new
            {
                id = Guid.NewGuid().ToString(),
                employeeId = employeeId,
                skillName = dto.SkillName,
                category = dto.Category,
                proficiencyLevel = dto.ProficiencyLevel,
                lastAssessedDate = DateTime.UtcNow,
                assessedBy = User.FindFirst("preferred_username")?.Value,
                notes = dto.Notes,
                createdAt = DateTime.UtcNow
            };

            return Ok(newSkill);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding skill for employee {EmployeeId}", employeeId);
            return StatusCode(500, new { message = "Failed to add skill" });
        }
    }

    /// <summary>
    /// Update employee skill
    /// </summary>
    [HttpPut("{skillId}")]
    [Authorize(Policy = "ManagerOrHR")]
    public async Task<IActionResult> UpdateEmployeeSkill(string employeeId, string skillId, [FromBody] UpdateSkillDto dto)
    {
        try
        {
            var updatedSkill = new
            {
                id = skillId,
                employeeId = employeeId,
                proficiencyLevel = dto.ProficiencyLevel,
                lastAssessedDate = DateTime.UtcNow,
                assessedBy = User.FindFirst("preferred_username")?.Value,
                notes = dto.Notes,
                updatedAt = DateTime.UtcNow
            };

            return Ok(updatedSkill);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating skill {SkillId}", skillId);
            return StatusCode(500, new { message = "Failed to update skill" });
        }
    }

    /// <summary>
    /// Delete employee skill
    /// </summary>
    [HttpDelete("{skillId}")]
    [Authorize(Policy = "HRStaff")]
    public async Task<IActionResult> DeleteEmployeeSkill(string employeeId, string skillId)
    {
        try
        {
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting skill {SkillId}", skillId);
            return StatusCode(500, new { message = "Failed to delete skill" });
        }
    }
}

public record AddSkillDto(string SkillName, string Category, int ProficiencyLevel, string? Notes);
public record UpdateSkillDto(int ProficiencyLevel, string? Notes);
