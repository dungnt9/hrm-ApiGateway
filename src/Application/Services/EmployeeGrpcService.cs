using ApiGateway.Protos;
using Grpc.Net.ClientFactory;

namespace ApiGateway.Services;

public interface IEmployeeGrpcService
{
    Task<EmployeeResponse> GetEmployeeAsync(string employeeId);
    Task<EmployeesResponse> GetEmployeesAsync(int page, int pageSize, string? departmentId, string? teamId, string? search, string? status = null);
    Task<EmployeeResponse> CreateEmployeeAsync(CreateEmployeeRequest request);
    Task<EmployeeResponse> UpdateEmployeeAsync(UpdateEmployeeRequest request);
    Task<DeleteEmployeeResponse> DeleteEmployeeAsync(string employeeId);
    Task<OrgChartResponse> GetOrgChartAsync(string? rootId, int depth);
    Task<EmployeesResponse> GetTeamMembersAsync(string? teamId, string? managerId);
    Task<EmployeeResponse> GetEmployeeManagerAsync(string employeeId);
    Task<ValidateManagerPermissionResponse> ValidateManagerPermissionAsync(string managerId, string employeeId);
    Task<AssignRoleResponse> AssignRoleAsync(string employeeId, string role);
    Task<DepartmentsResponse> GetDepartmentsAsync(string? companyId);
    Task<Department> GetDepartmentAsync(string departmentId);
    Task<Department> CreateDepartmentAsync(CreateDepartmentRequest request);
    Task<Department> UpdateDepartmentAsync(UpdateDepartmentRequest request);
    Task<DeleteDepartmentResponse> DeleteDepartmentAsync(string departmentId);
    Task<TeamsResponse> GetTeamsAsync(string? departmentId);
    Task<Team> GetTeamAsync(string teamId);
    Task<Team> CreateTeamAsync(CreateTeamRequest request);
    Task<Team> UpdateTeamAsync(UpdateTeamRequest request);
    Task<DeleteTeamResponse> DeleteTeamAsync(string teamId);
    Task<EmployeeResponse> GetEmployeeByKeycloakIdAsync(string keycloakUserId);
}

public class EmployeeGrpcService : IEmployeeGrpcService
{
    private readonly EmployeeGrpc.EmployeeGrpcClient _client;
    private readonly ILogger<EmployeeGrpcService> _logger;

    public EmployeeGrpcService(GrpcClientFactory grpcClientFactory, ILogger<EmployeeGrpcService> logger)
    {
        _client = grpcClientFactory.CreateClient<EmployeeGrpc.EmployeeGrpcClient>("EmployeeService");
        _logger = logger;
    }

    public async Task<EmployeeResponse> GetEmployeeAsync(string employeeId)
    {
        var request = new GetEmployeeRequest { EmployeeId = employeeId };
        return await _client.GetEmployeeAsync(request);
    }

    public async Task<EmployeesResponse> GetEmployeesAsync(int page, int pageSize, string? departmentId, string? teamId, string? search, string? status = null)
    {
        var request = new GetEmployeesRequest
        {
            Page = page,
            PageSize = pageSize,
            DepartmentId = departmentId ?? "",
            TeamId = teamId ?? "",
            Search = search ?? "",
            Status = status ?? ""
        };
        return await _client.GetEmployeesAsync(request);
    }

    public async Task<EmployeeResponse> CreateEmployeeAsync(CreateEmployeeRequest request)
    {
        return await _client.CreateEmployeeAsync(request);
    }

    public async Task<EmployeeResponse> UpdateEmployeeAsync(UpdateEmployeeRequest request)
    {
        return await _client.UpdateEmployeeAsync(request);
    }

    public async Task<DeleteEmployeeResponse> DeleteEmployeeAsync(string employeeId)
    {
        var request = new DeleteEmployeeRequest { EmployeeId = employeeId };
        return await _client.DeleteEmployeeAsync(request);
    }

    public async Task<OrgChartResponse> GetOrgChartAsync(string? rootId, int depth)
    {
        var request = new GetOrgChartRequest
        {
            RootId = rootId ?? "",
            Depth = depth
        };
        return await _client.GetOrgChartAsync(request);
    }

    public async Task<EmployeesResponse> GetTeamMembersAsync(string? teamId, string? managerId)
    {
        var request = new GetTeamMembersRequest
        {
            TeamId = teamId ?? "",
            ManagerId = managerId ?? ""
        };
        return await _client.GetTeamMembersAsync(request);
    }

    public async Task<EmployeeResponse> GetEmployeeManagerAsync(string employeeId)
    {
        var request = new GetEmployeeManagerRequest { EmployeeId = employeeId };
        return await _client.GetEmployeeManagerAsync(request);
    }

    public async Task<ValidateManagerPermissionResponse> ValidateManagerPermissionAsync(string managerId, string employeeId)
    {
        var request = new ValidateManagerPermissionRequest
        {
            ManagerId = managerId,
            EmployeeId = employeeId
        };
        return await _client.ValidateManagerPermissionAsync(request);
    }

    public async Task<AssignRoleResponse> AssignRoleAsync(string employeeId, string role)
    {
        var request = new AssignRoleRequest
        {
            EmployeeId = employeeId,
            Role = role
        };
        return await _client.AssignRoleAsync(request);
    }

    public async Task<DepartmentsResponse> GetDepartmentsAsync(string? companyId)
    {
        var request = new GetDepartmentsRequest { CompanyId = companyId ?? "" };
        return await _client.GetDepartmentsAsync(request);
    }

    public async Task<Department> GetDepartmentAsync(string departmentId)
    {
        var request = new GetDepartmentRequest { DepartmentId = departmentId };
        return await _client.GetDepartmentAsync(request);
    }

    public async Task<Department> CreateDepartmentAsync(CreateDepartmentRequest request)
    {
        return await _client.CreateDepartmentAsync(request);
    }

    public async Task<Department> UpdateDepartmentAsync(UpdateDepartmentRequest request)
    {
        return await _client.UpdateDepartmentAsync(request);
    }

    public async Task<DeleteDepartmentResponse> DeleteDepartmentAsync(string departmentId)
    {
        var request = new DeleteDepartmentRequest { DepartmentId = departmentId };
        return await _client.DeleteDepartmentAsync(request);
    }

    public async Task<TeamsResponse> GetTeamsAsync(string? departmentId)
    {
        var request = new GetTeamsRequest { DepartmentId = departmentId ?? "" };
        return await _client.GetTeamsAsync(request);
    }

    public async Task<Team> GetTeamAsync(string teamId)
    {
        var request = new GetTeamRequest { TeamId = teamId };
        return await _client.GetTeamAsync(request);
    }

    public async Task<Team> CreateTeamAsync(CreateTeamRequest request)
    {
        return await _client.CreateTeamAsync(request);
    }

    public async Task<Team> UpdateTeamAsync(UpdateTeamRequest request)
    {
        return await _client.UpdateTeamAsync(request);
    }

    public async Task<DeleteTeamResponse> DeleteTeamAsync(string teamId)
    {
        var request = new DeleteTeamRequest { TeamId = teamId };
        return await _client.DeleteTeamAsync(request);
    }

    public async Task<EmployeeResponse> GetEmployeeByKeycloakIdAsync(string keycloakUserId)
    {
        var request = new GetEmployeeByKeycloakIdRequest { KeycloakUserId = keycloakUserId };
        return await _client.GetEmployeeByKeycloakIdAsync(request);
    }
}
