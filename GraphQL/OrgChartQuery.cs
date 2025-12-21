using ApiGateway.Services;
using HotChocolate.Authorization;

namespace ApiGateway.GraphQL;

public class OrgChartQuery
{
    [Authorize]
    public async Task<OrgChartNode> GetOrgChart(
        [Service] IEmployeeGrpcService employeeService,
        string? rootId = null,
        int depth = 3)
    {
        var response = await employeeService.GetOrgChartAsync(rootId, depth);
        return MapToGraphQLNode(response.Root);
    }

    [Authorize]
    public async Task<IEnumerable<DepartmentNode>> GetDepartments(
        [Service] IEmployeeGrpcService employeeService,
        string? companyId = null)
    {
        var response = await employeeService.GetDepartmentsAsync(companyId);
        return response.Departments.Select(d => new DepartmentNode
        {
            Id = d.Id,
            Name = d.Name,
            CompanyId = d.CompanyId,
            ManagerId = d.ManagerId,
            ManagerName = d.ManagerName,
            CreatedAt = d.CreatedAt
        });
    }

    [Authorize]
    public async Task<IEnumerable<TeamNode>> GetTeams(
        [Service] IEmployeeGrpcService employeeService,
        string? departmentId = null)
    {
        var response = await employeeService.GetTeamsAsync(departmentId);
        return response.Teams.Select(t => new TeamNode
        {
            Id = t.Id,
            Name = t.Name,
            DepartmentId = t.DepartmentId,
            ManagerId = t.ManagerId,
            ManagerName = t.ManagerName,
            CreatedAt = t.CreatedAt
        });
    }

    [Authorize]
    public async Task<IEnumerable<EmployeeNode>> GetTeamMembers(
        [Service] IEmployeeGrpcService employeeService,
        string? teamId = null,
        string? managerId = null)
    {
        var response = await employeeService.GetTeamMembersAsync(teamId, managerId);
        return response.Employees.Select(e => new EmployeeNode
        {
            Id = e.Id,
            FirstName = e.FirstName,
            LastName = e.LastName,
            Email = e.Email,
            Phone = e.Phone,
            DepartmentId = e.DepartmentId,
            DepartmentName = e.DepartmentName,
            TeamId = e.TeamId,
            TeamName = e.TeamName,
            Position = e.Position,
            ManagerId = e.ManagerId,
            ManagerName = e.ManagerName,
            Status = e.Status
        });
    }

    private OrgChartNode MapToGraphQLNode(Protos.OrgChartNode protoNode)
    {
        var node = new OrgChartNode
        {
            Id = protoNode.Id,
            Name = protoNode.Name,
            Type = protoNode.Type,
            ParentId = protoNode.ParentId,
            Children = protoNode.Children.Select(MapToGraphQLNode).ToList()
        };

        if (protoNode.EmployeeData != null && !string.IsNullOrEmpty(protoNode.EmployeeData.Id))
        {
            node.EmployeeData = new EmployeeNode
            {
                Id = protoNode.EmployeeData.Id,
                FirstName = protoNode.EmployeeData.FirstName,
                LastName = protoNode.EmployeeData.LastName,
                Email = protoNode.EmployeeData.Email,
                Phone = protoNode.EmployeeData.Phone,
                Position = protoNode.EmployeeData.Position,
                DepartmentId = protoNode.EmployeeData.DepartmentId,
                DepartmentName = protoNode.EmployeeData.DepartmentName,
                TeamId = protoNode.EmployeeData.TeamId,
                TeamName = protoNode.EmployeeData.TeamName,
                ManagerId = protoNode.EmployeeData.ManagerId,
                ManagerName = protoNode.EmployeeData.ManagerName,
                Status = protoNode.EmployeeData.Status
            };
        }

        return node;
    }
}

public class OrgChartNode
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // company, department, team, employee
    public string ParentId { get; set; } = string.Empty;
    public List<OrgChartNode> Children { get; set; } = new();
    public EmployeeNode? EmployeeData { get; set; }
}

public class DepartmentNode
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CompanyId { get; set; } = string.Empty;
    public string ManagerId { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

public class TeamNode
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DepartmentId { get; set; } = string.Empty;
    public string ManagerId { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

public class EmployeeNode
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string DepartmentId { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string TeamId { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string ManagerId { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
