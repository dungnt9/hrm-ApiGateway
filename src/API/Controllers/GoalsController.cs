using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/employees/{employeeId}/goals")]
[Authorize]
public class GoalsController : ControllerBase
{
    private readonly ILogger<GoalsController> _logger;

    public GoalsController(ILogger<GoalsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get all goals for an employee
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetEmployeeGoals(string employeeId)
    {
        try
        {
            // Mock data - will be replaced with actual database query
            var goals = new List<object>
            {
                new
                {
                    id = Guid.NewGuid().ToString(),
                    employeeId = employeeId,
                    title = "Complete Advanced C# Certification",
                    description = "Obtain Microsoft Certified: Azure Developer Associate certification",
                    goalType = "Individual",
                    category = "Development",
                    startDate = DateTime.UtcNow.AddMonths(-2),
                    targetDate = DateTime.UtcNow.AddMonths(4),
                    completedDate = (DateTime?)null,
                    progress = 45,
                    status = "InProgress",
                    priority = "High",
                    metrics = "Pass certification exam with score >= 700",
                    notes = "Study materials provided by company",
                    reviewerId = (string?)null,
                    createdAt = DateTime.UtcNow.AddMonths(-2)
                },
                new
                {
                    id = Guid.NewGuid().ToString(),
                    employeeId = employeeId,
                    title = "Lead Q2 Project",
                    description = "Successfully lead and deliver the new CRM module",
                    goalType = "Individual",
                    category = "Performance",
                    startDate = new DateTime(2026, 4, 1),
                    targetDate = new DateTime(2026, 6, 30),
                    completedDate = (DateTime?)null,
                    progress = 0,
                    status = "NotStarted",
                    priority = "High",
                    metrics = "On-time delivery, within budget, positive team feedback",
                    notes = "Project kickoff scheduled for April 1st",
                    reviewerId = (string?)null,
                    createdAt = DateTime.UtcNow.AddDays(-14)
                },
                new
                {
                    id = Guid.NewGuid().ToString(),
                    employeeId = employeeId,
                    title = "Improve Code Review Quality",
                    description = "Provide thorough and constructive code reviews",
                    goalType = "Team",
                    category = "Development",
                    startDate = new DateTime(2026, 1, 1),
                    targetDate = new DateTime(2026, 12, 31),
                    completedDate = (DateTime?)null,
                    progress = 30,
                    status = "InProgress",
                    priority = "Medium",
                    metrics = "Average 3+ meaningful comments per review, team satisfaction >= 4/5",
                    notes = "Focus on architecture and best practices",
                    reviewerId = (string?)null,
                    createdAt = new DateTime(2026, 1, 1)
                }
            };

            return Ok(goals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching goals for employee {EmployeeId}", employeeId);
            return StatusCode(500, new { message = "Failed to fetch goals" });
        }
    }

    /// <summary>
    /// Create a new goal
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateGoal(string employeeId, [FromBody] CreateGoalDto dto)
    {
        try
        {
            var newGoal = new
            {
                id = Guid.NewGuid().ToString(),
                employeeId = employeeId,
                title = dto.Title,
                description = dto.Description,
                goalType = dto.GoalType,
                category = dto.Category,
                startDate = dto.StartDate,
                targetDate = dto.TargetDate,
                completedDate = (DateTime?)null,
                progress = 0,
                status = "NotStarted",
                priority = dto.Priority,
                metrics = dto.Metrics,
                notes = "",
                reviewerId = (string?)null,
                createdAt = DateTime.UtcNow
            };

            return Ok(newGoal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating goal for employee {EmployeeId}", employeeId);
            return StatusCode(500, new { message = "Failed to create goal" });
        }
    }

    /// <summary>
    /// Update a goal
    /// </summary>
    [HttpPut("{goalId}")]
    public async Task<IActionResult> UpdateGoal(string employeeId, string goalId, [FromBody] UpdateGoalDto dto)
    {
        try
        {
            var updatedGoal = new
            {
                id = goalId,
                progress = dto.Progress,
                status = dto.Status,
                notes = dto.Notes,
                updatedAt = DateTime.UtcNow
            };

            return Ok(updatedGoal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating goal {GoalId}", goalId);
            return StatusCode(500, new { message = "Failed to update goal" });
        }
    }

    /// <summary>
    /// Delete a goal
    /// </summary>
    [HttpDelete("{goalId}")]
    [Authorize(Policy = "ManagerOrHR")]
    public async Task<IActionResult> DeleteGoal(string employeeId, string goalId)
    {
        try
        {
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting goal {GoalId}", goalId);
            return StatusCode(500, new { message = "Failed to delete goal" });
        }
    }
}

public record CreateGoalDto(
    string Title,
    string Description,
    string GoalType,
    string Category,
    DateTime StartDate,
    DateTime TargetDate,
    string Priority,
    string? Metrics
);

public record UpdateGoalDto(
    int Progress,
    string Status,
    string? Notes
);
