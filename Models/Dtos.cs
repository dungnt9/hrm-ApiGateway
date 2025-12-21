namespace ApiGateway.Models;

public class CreateEmployeeDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? DepartmentId { get; set; }
    public string? TeamId { get; set; }
    public string? Position { get; set; }
    public string? ManagerId { get; set; }
    public string? KeycloakUserId { get; set; }
    public string? HireDate { get; set; }
}

public class UpdateEmployeeDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? DepartmentId { get; set; }
    public string? TeamId { get; set; }
    public string? Position { get; set; }
    public string? ManagerId { get; set; }
    public string? Status { get; set; }
}

public class AssignRoleDto
{
    public string Role { get; set; } = string.Empty;
}

public class CheckInDto
{
    public string? EmployeeId { get; set; }
    public string? Note { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class CheckOutDto
{
    public string? EmployeeId { get; set; }
    public string? Note { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class CreateLeaveRequestDto
{
    public string? EmployeeId { get; set; }
    public string LeaveType { get; set; } = string.Empty; // annual, sick, unpaid
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string ApproverId { get; set; } = string.Empty;
    public string ApproverType { get; set; } = string.Empty; // manager, hr
}

public class ApproveLeaveRequestDto
{
    public string? Note { get; set; }
}

public class RejectLeaveRequestDto
{
    public string Reason { get; set; } = string.Empty;
}
