using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/employees/{employeeId}/certifications")]
[Authorize]
public class CertificationsController : ControllerBase
{
    private readonly ILogger<CertificationsController> _logger;

    public CertificationsController(ILogger<CertificationsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get all certifications for an employee
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetEmployeeCertifications(string employeeId)
    {
        try
        {
            // Mock data - will be replaced with actual database query
            var certifications = new List<object>
            {
                new
                {
                    id = Guid.NewGuid().ToString(),
                    employeeId = employeeId,
                    certificationName = "Microsoft Certified: Azure Developer Associate",
                    issuingOrganization = "Microsoft",
                    certificationNumber = "AZ-204-12345",
                    issueDate = new DateTime(2024, 6, 15),
                    expiryDate = new DateTime(2026, 6, 15),
                    requiresRenewal = true,
                    documentUrl = "",
                    status = "Active",
                    createdAt = new DateTime(2024, 6, 15)
                },
                new
                {
                    id = Guid.NewGuid().ToString(),
                    employeeId = employeeId,
                    certificationName = "AWS Certified Solutions Architect",
                    issuingOrganization = "Amazon Web Services",
                    certificationNumber = "AWS-SAA-67890",
                    issueDate = new DateTime(2023, 3, 10),
                    expiryDate = new DateTime(2026, 3, 10),
                    requiresRenewal = true,
                    documentUrl = "",
                    status = "Active",
                    createdAt = new DateTime(2023, 3, 10)
                },
                new
                {
                    id = Guid.NewGuid().ToString(),
                    employeeId = employeeId,
                    certificationName = "Professional Scrum Master I (PSM I)",
                    issuingOrganization = "Scrum.org",
                    certificationNumber = "PSM-54321",
                    issueDate = new DateTime(2022, 9, 5),
                    expiryDate = (DateTime?)null, // No expiry
                    requiresRenewal = false,
                    documentUrl = "",
                    status = "Active",
                    createdAt = new DateTime(2022, 9, 5)
                }
            };

            return Ok(certifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching certifications for employee {EmployeeId}", employeeId);
            return StatusCode(500, new { message = "Failed to fetch certifications" });
        }
    }

    /// <summary>
    /// Add a new certification
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "ManagerOrHR")]
    public async Task<IActionResult> AddCertification(string employeeId, [FromBody] CreateCertificationDto dto)
    {
        try
        {
            var newCertification = new
            {
                id = Guid.NewGuid().ToString(),
                employeeId = employeeId,
                certificationName = dto.CertificationName,
                issuingOrganization = dto.IssuingOrganization,
                certificationNumber = dto.CertificationNumber,
                issueDate = dto.IssueDate,
                expiryDate = dto.ExpiryDate,
                requiresRenewal = dto.RequiresRenewal,
                documentUrl = "",
                status = "Active",
                createdAt = DateTime.UtcNow
            };

            return Ok(newCertification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding certification for employee {EmployeeId}", employeeId);
            return StatusCode(500, new { message = "Failed to add certification" });
        }
    }

    /// <summary>
    /// Update a certification
    /// </summary>
    [HttpPut("{certificationId}")]
    [Authorize(Policy = "ManagerOrHR")]
    public async Task<IActionResult> UpdateCertification(string employeeId, string certificationId, [FromBody] UpdateCertificationDto dto)
    {
        try
        {
            var updatedCertification = new
            {
                id = certificationId,
                expiryDate = dto.ExpiryDate,
                status = dto.Status,
                updatedAt = DateTime.UtcNow
            };

            return Ok(updatedCertification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating certification {CertificationId}", certificationId);
            return StatusCode(500, new { message = "Failed to update certification" });
        }
    }

    /// <summary>
    /// Delete a certification
    /// </summary>
    [HttpDelete("{certificationId}")]
    [Authorize(Policy = "HRStaff")]
    public async Task<IActionResult> DeleteCertification(string employeeId, string certificationId)
    {
        try
        {
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting certification {CertificationId}", certificationId);
            return StatusCode(500, new { message = "Failed to delete certification" });
        }
    }
}

public record CreateCertificationDto(
    string CertificationName,
    string IssuingOrganization,
    string? CertificationNumber,
    DateTime IssueDate,
    DateTime? ExpiryDate,
    bool RequiresRenewal
);

public record UpdateCertificationDto(
    DateTime? ExpiryDate,
    string Status
);
