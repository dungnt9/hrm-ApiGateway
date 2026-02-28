using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/employees/{employeeId}/performance-reviews")]
[Authorize(Policy = "ManagerOrHR")]
public class PerformanceReviewsController : ControllerBase
{
    private readonly ILogger<PerformanceReviewsController> _logger;

    public PerformanceReviewsController(ILogger<PerformanceReviewsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get all performance reviews for an employee
    /// </summary>
    [HttpGet]
    [Authorize] // Any authenticated user can view their own reviews
    public async Task<IActionResult> GetPerformanceReviews(string employeeId)
    {
        try
        {
            // Mock data - will be replaced with actual database query
            var reviews = new List<object>
            {
                new
                {
                    id = Guid.NewGuid().ToString(),
                    employeeId = employeeId,
                    reviewerId = Guid.NewGuid().ToString(),
                    reviewType = "Annual",
                    reviewPeriodStart = new DateTime(2025, 1, 1),
                    reviewPeriodEnd = new DateTime(2025, 12, 31),
                    reviewDate = DateTime.UtcNow.AddMonths(-1),
                    performanceRating = 4,
                    behaviorRating = 4,
                    teamworkRating = 5,
                    initiativeRating = 4,
                    overallRating = 4,
                    strengths = "Excellent technical skills and team collaboration",
                    areasForImprovement = "Time management could be improved",
                    goals = "Lead a major project in Q2",
                    reviewerComments = "Great performance overall",
                    employeeComments = "Thank you for the feedback",
                    status = "Acknowledged",
                    employeeAcknowledgedAt = DateTime.UtcNow.AddDays(-5),
                    createdAt = DateTime.UtcNow.AddMonths(-1)
                }
            };

            return Ok(reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching performance reviews for employee {EmployeeId}", employeeId);
            return StatusCode(500, new { message = "Failed to fetch performance reviews" });
        }
    }

    /// <summary>
    /// Create a new performance review
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreatePerformanceReview(string employeeId, [FromBody] CreatePerformanceReviewDto dto)
    {
        try
        {
            var newReview = new
            {
                id = Guid.NewGuid().ToString(),
                employeeId = employeeId,
                reviewerId = User.FindFirst("sub")?.Value,
                reviewType = dto.ReviewType,
                reviewPeriodStart = dto.ReviewPeriodStart,
                reviewPeriodEnd = dto.ReviewPeriodEnd,
                reviewDate = DateTime.UtcNow,
                performanceRating = dto.PerformanceRating,
                behaviorRating = dto.BehaviorRating,
                teamworkRating = dto.TeamworkRating,
                initiativeRating = dto.InitiativeRating,
                overallRating = dto.OverallRating,
                strengths = dto.Strengths,
                areasForImprovement = dto.AreasForImprovement,
                goals = dto.Goals,
                reviewerComments = dto.ReviewerComments,
                status = "Submitted",
                createdAt = DateTime.UtcNow
            };

            return Ok(newReview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating performance review for employee {EmployeeId}", employeeId);
            return StatusCode(500, new { message = "Failed to create performance review" });
        }
    }

    /// <summary>
    /// Update a performance review
    /// </summary>
    [HttpPut("{reviewId}")]
    public async Task<IActionResult> UpdatePerformanceReview(string employeeId, string reviewId, [FromBody] UpdatePerformanceReviewDto dto)
    {
        try
        {
            var updatedReview = new
            {
                id = reviewId,
                performanceRating = dto.PerformanceRating,
                behaviorRating = dto.BehaviorRating,
                teamworkRating = dto.TeamworkRating,
                initiativeRating = dto.InitiativeRating,
                overallRating = dto.OverallRating,
                strengths = dto.Strengths,
                areasForImprovement = dto.AreasForImprovement,
                goals = dto.Goals,
                reviewerComments = dto.ReviewerComments,
                status = dto.Status,
                updatedAt = DateTime.UtcNow
            };

            return Ok(updatedReview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating performance review {ReviewId}", reviewId);
            return StatusCode(500, new { message = "Failed to update performance review" });
        }
    }

    /// <summary>
    /// Employee acknowledges performance review
    /// </summary>
    [HttpPost("{reviewId}/acknowledge")]
    [Authorize]
    public async Task<IActionResult> AcknowledgeReview(string employeeId, string reviewId, [FromBody] AcknowledgeReviewDto dto)
    {
        try
        {
            var acknowledgedReview = new
            {
                id = reviewId,
                employeeComments = dto.EmployeeComments,
                employeeAcknowledgedAt = DateTime.UtcNow,
                status = "Acknowledged"
            };

            return Ok(acknowledgedReview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging performance review {ReviewId}", reviewId);
            return StatusCode(500, new { message = "Failed to acknowledge review" });
        }
    }

    /// <summary>
    /// Delete a performance review
    /// </summary>
    [HttpDelete("{reviewId}")]
    [Authorize(Policy = "HRStaff")]
    public async Task<IActionResult> DeletePerformanceReview(string employeeId, string reviewId)
    {
        try
        {
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting performance review {ReviewId}", reviewId);
            return StatusCode(500, new { message = "Failed to delete performance review" });
        }
    }
}

public record CreatePerformanceReviewDto(
    string ReviewType,
    DateTime ReviewPeriodStart,
    DateTime ReviewPeriodEnd,
    int? PerformanceRating,
    int? BehaviorRating,
    int? TeamworkRating,
    int? InitiativeRating,
    int? OverallRating,
    string? Strengths,
    string? AreasForImprovement,
    string? Goals,
    string? ReviewerComments
);

public record UpdatePerformanceReviewDto(
    int? PerformanceRating,
    int? BehaviorRating,
    int? TeamworkRating,
    int? InitiativeRating,
    int? OverallRating,
    string? Strengths,
    string? AreasForImprovement,
    string? Goals,
    string? ReviewerComments,
    string? Status
);

public record AcknowledgeReviewDto(string? EmployeeComments);
