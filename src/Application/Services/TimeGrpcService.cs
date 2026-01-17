using ApiGateway.Protos;
using Grpc.Net.ClientFactory;

namespace ApiGateway.Services;

public interface ITimeGrpcService
{
    Task<CheckInResponse> CheckInAsync(CheckInRequest request);
    Task<CheckOutResponse> CheckOutAsync(CheckOutRequest request);
    Task<AttendanceHistoryResponse> GetAttendanceHistoryAsync(string employeeId, string? startDate, string? endDate, int page, int pageSize);
    Task<AttendanceStatusResponse> GetAttendanceStatusAsync(string employeeId, string? date);
    Task<LeaveRequestResponse> CreateLeaveRequestAsync(CreateLeaveRequestRequest request);
    Task<LeaveRequestsResponse> GetLeaveRequestsAsync(string? employeeId, string? approverId, string? status, string? leaveType, string? startDate, string? endDate, int page, int pageSize);
    Task<LeaveRequestResponse> GetLeaveRequestDetailAsync(string leaveRequestId);
    Task<LeaveRequestResponse> ApproveLeaveRequestAsync(string leaveRequestId, string approverId, string? note);
    Task<LeaveRequestResponse> RejectLeaveRequestAsync(string leaveRequestId, string approverId, string reason);
    Task<LeaveBalanceResponse> GetLeaveBalanceAsync(string employeeId, int year);
    Task<ShiftsResponse> GetShiftsAsync(string? departmentId);
    Task<ShiftResponse> GetEmployeeShiftAsync(string employeeId, string? date);
    Task<OvertimeRequestResponse> CreateOvertimeRequestAsync(CreateOvertimeRequestRequest request);
    Task<OvertimeRequestsResponse> GetOvertimeRequestsAsync(string? employeeId, string? status, string? startDate, string? endDate, int page, int pageSize);
    Task<OvertimeRequestResponse> GetOvertimeRequestDetailAsync(string overtimeRequestId);
    Task<OvertimeRequestResponse> ApproveOvertimeRequestAsync(string overtimeRequestId, string approverId, string? comment);
    Task<OvertimeRequestResponse> RejectOvertimeRequestAsync(string overtimeRequestId, string approverId, string reason);
}

public class TimeGrpcService : ITimeGrpcService
{
    private readonly TimeGrpc.TimeGrpcClient _client;
    private readonly ILogger<TimeGrpcService> _logger;

    public TimeGrpcService(GrpcClientFactory grpcClientFactory, ILogger<TimeGrpcService> logger)
    {
        _client = grpcClientFactory.CreateClient<TimeGrpc.TimeGrpcClient>("TimeService");
        _logger = logger;
    }

    public async Task<CheckInResponse> CheckInAsync(CheckInRequest request)
    {
        return await _client.CheckInAsync(request);
    }

    public async Task<CheckOutResponse> CheckOutAsync(CheckOutRequest request)
    {
        return await _client.CheckOutAsync(request);
    }

    public async Task<AttendanceHistoryResponse> GetAttendanceHistoryAsync(string employeeId, string? startDate, string? endDate, int page, int pageSize)
    {
        var request = new GetAttendanceHistoryRequest
        {
            EmployeeId = employeeId,
            StartDate = startDate ?? "",
            EndDate = endDate ?? "",
            Page = page,
            PageSize = pageSize
        };
        return await _client.GetAttendanceHistoryAsync(request);
    }

    public async Task<AttendanceStatusResponse> GetAttendanceStatusAsync(string employeeId, string? date)
    {
        var request = new GetAttendanceStatusRequest
        {
            EmployeeId = employeeId,
            Date = date ?? ""
        };
        return await _client.GetAttendanceStatusAsync(request);
    }

    public async Task<LeaveRequestResponse> CreateLeaveRequestAsync(CreateLeaveRequestRequest request)
    {
        return await _client.CreateLeaveRequestAsync(request);
    }

    public async Task<LeaveRequestsResponse> GetLeaveRequestsAsync(string? employeeId, string? approverId, string? status, string? leaveType, string? startDate, string? endDate, int page, int pageSize)
    {
        var request = new GetLeaveRequestsRequest
        {
            EmployeeId = employeeId ?? "",
            ApproverId = approverId ?? "",
            Status = status ?? "",
            LeaveType = leaveType ?? "",
            StartDate = startDate ?? "",
            EndDate = endDate ?? "",
            Page = page,
            PageSize = pageSize
        };
        return await _client.GetLeaveRequestsAsync(request);
    }

    public async Task<LeaveRequestResponse> GetLeaveRequestDetailAsync(string leaveRequestId)
    {
        var request = new GetLeaveRequestDetailRequest { LeaveRequestId = leaveRequestId };
        return await _client.GetLeaveRequestDetailAsync(request);
    }

    public async Task<LeaveRequestResponse> ApproveLeaveRequestAsync(string leaveRequestId, string approverId, string? note)
    {
        var request = new ApproveLeaveRequestRequest
        {
            LeaveRequestId = leaveRequestId,
            ApproverId = approverId,
            Note = note ?? ""
        };
        return await _client.ApproveLeaveRequestAsync(request);
    }

    public async Task<LeaveRequestResponse> RejectLeaveRequestAsync(string leaveRequestId, string approverId, string reason)
    {
        var request = new RejectLeaveRequestRequest
        {
            LeaveRequestId = leaveRequestId,
            ApproverId = approverId,
            Reason = reason
        };
        return await _client.RejectLeaveRequestAsync(request);
    }

    public async Task<LeaveBalanceResponse> GetLeaveBalanceAsync(string employeeId, int year)
    {
        var request = new GetLeaveBalanceRequest
        {
            EmployeeId = employeeId,
            Year = year
        };
        return await _client.GetLeaveBalanceAsync(request);
    }

    public async Task<ShiftsResponse> GetShiftsAsync(string? departmentId)
    {
        var request = new GetShiftsRequest { DepartmentId = departmentId ?? "" };
        return await _client.GetShiftsAsync(request);
    }

    public async Task<ShiftResponse> GetEmployeeShiftAsync(string employeeId, string? date)
    {
        var request = new GetEmployeeShiftRequest
        {
            EmployeeId = employeeId,
            Date = date ?? ""
        };
        return await _client.GetEmployeeShiftAsync(request);
    }

    public async Task<OvertimeRequestResponse> CreateOvertimeRequestAsync(CreateOvertimeRequestRequest request)
    {
        return await _client.CreateOvertimeRequestAsync(request);
    }

    public async Task<OvertimeRequestsResponse> GetOvertimeRequestsAsync(string? employeeId, string? status, string? startDate, string? endDate, int page, int pageSize)
    {
        var request = new GetOvertimeRequestsRequest
        {
            EmployeeId = employeeId ?? "",
            Status = status ?? "",
            StartDate = startDate ?? "",
            EndDate = endDate ?? "",
            Page = page,
            PageSize = pageSize
        };
        return await _client.GetOvertimeRequestsAsync(request);
    }

    public async Task<OvertimeRequestResponse> GetOvertimeRequestDetailAsync(string overtimeRequestId)
    {
        var request = new GetOvertimeRequestDetailRequest { OvertimeRequestId = overtimeRequestId };
        return await _client.GetOvertimeRequestDetailAsync(request);
    }

    public async Task<OvertimeRequestResponse> ApproveOvertimeRequestAsync(string overtimeRequestId, string approverId, string? comment)
    {
        var request = new ApproveOvertimeRequestRequest
        {
            OvertimeRequestId = overtimeRequestId,
            ApproverId = approverId,
            Comment = comment ?? ""
        };
        return await _client.ApproveOvertimeRequestAsync(request);
    }

    public async Task<OvertimeRequestResponse> RejectOvertimeRequestAsync(string overtimeRequestId, string approverId, string reason)
    {
        var request = new RejectOvertimeRequestRequest
        {
            OvertimeRequestId = overtimeRequestId,
            ApproverId = approverId,
            Reason = reason
        };
        return await _client.RejectOvertimeRequestAsync(request);
    }
}
