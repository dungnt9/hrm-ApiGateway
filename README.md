# API Gateway

REST/GraphQL gateway cho hệ thống HRM - Entry point cho tất cả client requests.

## Mục lục

- [Kiến trúc](#kiến-trúc)
- [Công nghệ](#công-nghệ)
- [Nghiệp vụ](#nghiệp-vụ)
- [REST API](#rest-api)
- [GraphQL API](#graphql-api)
- [Authentication & Authorization](#authentication--authorization)
- [gRPC Clients](#grpc-clients)
- [Luồng xử lý](#luồng-xử-lý)
- [Cấu hình](#cấu-hình)
- [Chạy ứng dụng](#chạy-ứng-dụng)

---

## Kiến trúc

**API Gateway Pattern - Aggregator:**

```
src/
├── API/                        # Presentation Layer
│   ├── Configuration/          # Service configs
│   ├── Controllers/            # REST API controllers
│   │   ├── AuthController.cs       # Authentication endpoints
│   │   ├── EmployeesController.cs  # Employee management
│   │   ├── AttendanceController.cs # Attendance tracking
│   │   ├── LeaveController.cs      # Leave requests
│   │   ├── OvertimeController.cs   # Overtime requests
│   │   └── NotificationsController.cs # Notifications proxy
│   ├── GraphQL/                # GraphQL queries
│   │   └── OrgChartQuery.cs    # Organization chart
│   ├── Hubs/                   # SignalR hub proxy
│   ├── Models/                 # DTOs
│   ├── Protos/                 # gRPC proto files
│   └── Program.cs              # Entry point & DI
│
└── Application/                # Business Logic Layer
    └── Services/               # gRPC client services
        ├── EmployeeGrpcService.cs  # Employee Service client
        └── TimeGrpcService.cs      # Time Service client
```

---

## Công nghệ

| Công nghệ | Phiên bản | Mục đích |
|-----------|-----------|----------|
| .NET | 8.0 | Framework |
| ASP.NET Core | 8.0 | Web framework |
| HotChocolate | 13.x | GraphQL server |
| Grpc.Net.Client | - | gRPC client |
| SignalR | 8.0 | WebSocket proxy |
| Keycloak | 23.0 | JWT Authentication & RBAC |
| FluentValidation | 11.x | Input validation |
| Serilog | - | Structured logging |
| Swashbuckle | - | Swagger/OpenAPI |

---

## Nghiệp vụ

### Vai trò của API Gateway

| Chức năng | Mô tả |
|-----------|-------|
| **Routing** | Điều hướng requests đến các microservices |
| **Authentication** | Xác thực JWT tokens từ Keycloak |
| **Authorization** | Kiểm tra quyền truy cập theo role |
| **Aggregation** | Gộp dữ liệu từ nhiều services |
| **Protocol Translation** | REST → gRPC, HTTP → WebSocket |
| **API Documentation** | Swagger UI cho developers |

### Service Dependencies

| Service | Protocol | Chức năng |
|---------|----------|-----------|
| Employee Service | gRPC | Quản lý nhân viên, phòng ban, team |
| Time Service | gRPC | Chấm công, nghỉ phép, tăng ca |
| Notification Service | HTTP | Thông báo real-time |
| Keycloak | HTTP | Authentication & Authorization |

---

## REST API

### Authentication (`/api/auth`)

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| `POST` | `/login` | Đăng nhập với username/password | - |
| `POST` | `/refresh` | Làm mới JWT token | - |
| `POST` | `/logout` | Đăng xuất | Required |
| `GET` | `/me` | Lấy thông tin user hiện tại | Required |
| `POST` | `/change-password` | Đổi mật khẩu | Required |

**Login Request:**
```json
{
  "username": "employee@hrm.vn",
  "password": "password123"
}
```

**Login Response:**
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIs...",
  "refreshToken": "eyJhbGciOiJIUzI1NiIs...",
  "expiresIn": 300,
  "tokenType": "Bearer"
}
```

### Employees (`/api/employees`)

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| `GET` | `/` | Danh sách nhân viên (phân trang, filter) | Employee |
| `GET` | `/{id}` | Chi tiết nhân viên | Employee |
| `POST` | `/` | Tạo nhân viên mới | HRStaff |
| `PUT` | `/{id}` | Cập nhật nhân viên | HRStaff |
| `DELETE` | `/{id}` | Xóa nhân viên | Admin |
| `GET` | `/{id}/manager` | Lấy thông tin quản lý | Employee |
| `GET` | `/team/{teamId}` | Danh sách thành viên team | ManagerOrHR |
| `GET` | `/manager/{managerId}/team` | Nhân viên do manager quản lý | ManagerOrHR |
| `POST` | `/{id}/assign-role` | Gán vai trò cho nhân viên | Admin |
| `GET` | `/departments` | Danh sách phòng ban | Employee |
| `GET` | `/teams` | Danh sách team | Employee |

**Query Parameters:**
```
GET /api/employees?page=1&pageSize=10&departmentId=xxx&teamId=xxx&search=keyword
```

### Attendance (`/api/attendance`)

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| `POST` | `/check-in` | Chấm công vào | Employee |
| `POST` | `/check-out` | Chấm công ra | Employee |
| `GET` | `/status` | Trạng thái chấm công hôm nay | Employee |
| `GET` | `/history` | Lịch sử chấm công | Employee |
| `GET` | `/team/{teamId}` | Chấm công team theo ngày | ManagerOrHR |
| `GET` | `/shifts` | Danh sách ca làm việc | Employee |
| `GET` | `/shift` | Ca làm việc của nhân viên | Employee |

**Check-in Request:**
```json
{
  "latitude": 21.028511,
  "longitude": 105.804817,
  "deviceInfo": "Chrome/Windows",
  "note": "Work from office"
}
```

### Leave Management (`/api/leave`)

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| `POST` | `/request` | Tạo đơn nghỉ phép | Employee |
| `GET` | `/requests` | Danh sách đơn của mình | Employee |
| `GET` | `/requests/pending` | Đơn chờ duyệt (team) | ManagerOrHR |
| `GET` | `/request/{id}` | Chi tiết đơn nghỉ | Employee |
| `POST` | `/request/{id}/approve` | Duyệt đơn nghỉ | ManagerOrHR |
| `POST` | `/request/{id}/reject` | Từ chối đơn nghỉ | ManagerOrHR |
| `GET` | `/balance` | Số ngày phép còn lại | Employee |

**Create Leave Request:**
```json
{
  "leaveType": "Annual",
  "startDate": "2025-01-20",
  "endDate": "2025-01-22",
  "reason": "Family vacation"
}
```

### Overtime (`/api/overtime`)

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| `POST` | `/request` | Tạo đơn tăng ca | Employee |
| `GET` | `/requests` | Danh sách đơn OT | Employee |
| `GET` | `/requests/pending` | Đơn OT chờ duyệt | ManagerOrHR |
| `GET` | `/request/{id}` | Chi tiết đơn OT | Employee |
| `POST` | `/request/{id}/approve` | Duyệt đơn OT | ManagerOrHR |
| `POST` | `/request/{id}/reject` | Từ chối đơn OT | ManagerOrHR |

### Notifications (`/api/notifications`)

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| `GET` | `/` | Danh sách thông báo | Employee |
| `POST` | `/{id}/read` | Đánh dấu đã đọc | Employee |
| `POST` | `/read-all` | Đọc tất cả thông báo | Employee |
| `GET` | `/templates` | Notification templates | Admin |
| `GET` | `/preferences` | Preferences của user | Employee |
| `PUT` | `/preferences` | Cập nhật preferences | Employee |

---

## GraphQL API

### Endpoint

```
POST /graphql
```

### GraphQL Playground

```
GET /graphql
```

### Queries

#### Organization Chart

```graphql
query GetOrgChart($rootId: String, $depth: Int) {
  getOrgChart(rootId: $rootId, depth: $depth) {
    id
    name
    type
    parentId
    children {
      id
      name
      type
      employeeData {
        id
        firstName
        lastName
        position
        email
      }
    }
  }
}
```

#### Departments

```graphql
query GetDepartments($companyId: String) {
  getDepartments(companyId: $companyId) {
    id
    name
    managerId
    managerName
    createdAt
  }
}
```

#### Teams

```graphql
query GetTeams($departmentId: String!) {
  getTeams(departmentId: $departmentId) {
    id
    name
    departmentId
    managerId
    managerName
    createdAt
  }
}
```

#### Team Members

```graphql
query GetTeamMembers($teamId: String, $managerId: String) {
  getTeamMembers(teamId: $teamId, managerId: $managerId) {
    employees {
      id
      firstName
      lastName
      email
      position
      status
    }
    totalCount
  }
}
```

---

## Authentication & Authorization

### Keycloak Configuration

```json
{
  "Keycloak": {
    "Authority": "http://keycloak:8080/realms/hrm",
    "Audience": "hrm-api",
    "ClientId": "hrm-api",
    "ClientSecret": "hrm-api-secret",
    "RequireHttps": false
  }
}
```

### Authorization Policies

| Policy | Required Role | Mô tả |
|--------|---------------|-------|
| `Employee` | `employee` | Quyền cơ bản cho nhân viên |
| `Manager` | `manager` | Quản lý team |
| `HRStaff` | `hr_staff` | Nghiệp vụ HR |
| `Admin` | `system_admin` | Full access |
| `ManagerOrHR` | `manager` OR `hr_staff` | Duyệt đơn |

### Role Hierarchy

```
system_admin
    └── hr_staff
           └── manager
                  └── employee
```

### JWT Token Example

```json
{
  "sub": "user-keycloak-id",
  "preferred_username": "employee@hrm.vn",
  "email": "employee@hrm.vn",
  "realm_access": {
    "roles": ["employee", "manager"]
  },
  "resource_access": {
    "hrm-api": {
      "roles": ["employee"]
    }
  }
}
```

---

## gRPC Clients

### Employee Service Client

```csharp
public interface IEmployeeGrpcService
{
    Task<EmployeeResponse> GetEmployeeAsync(string employeeId);
    Task<EmployeesResponse> GetEmployeesAsync(GetEmployeesRequest request);
    Task<EmployeeResponse> CreateEmployeeAsync(CreateEmployeeRequest request);
    Task<EmployeeResponse> UpdateEmployeeAsync(UpdateEmployeeRequest request);
    Task<DeleteEmployeeResponse> DeleteEmployeeAsync(string employeeId);
    Task<OrgChartResponse> GetOrgChartAsync(string? rootId, int depth);
    Task<EmployeesResponse> GetTeamMembersAsync(string? teamId, string? managerId);
    Task<EmployeeResponse> GetEmployeeManagerAsync(string employeeId);
    Task<ValidateManagerPermissionResponse> ValidateManagerPermissionAsync(string managerId, string employeeId);
    Task<AssignRoleResponse> AssignRoleAsync(string employeeId, string role);
    Task<DepartmentsResponse> GetDepartmentsAsync(string? companyId);
    Task<TeamsResponse> GetTeamsAsync(string? departmentId);
}
```

### Time Service Client

```csharp
public interface ITimeGrpcService
{
    // Attendance
    Task<AttendanceResponse> CheckInAsync(CheckInRequest request);
    Task<AttendanceResponse> CheckOutAsync(CheckOutRequest request);
    Task<AttendanceStatusResponse> GetAttendanceStatusAsync(string employeeId);
    Task<AttendanceHistoryResponse> GetAttendanceHistoryAsync(GetAttendanceHistoryRequest request);

    // Leave
    Task<LeaveRequestResponse> CreateLeaveRequestAsync(CreateLeaveRequest request);
    Task<LeaveRequestsResponse> GetLeaveRequestsAsync(GetLeaveRequestsRequest request);
    Task<LeaveRequestResponse> GetLeaveRequestDetailAsync(string requestId);
    Task<LeaveRequestResponse> ApproveLeaveRequestAsync(ApproveLeaveRequest request);
    Task<LeaveRequestResponse> RejectLeaveRequestAsync(RejectLeaveRequest request);
    Task<LeaveBalanceResponse> GetLeaveBalanceAsync(string employeeId, int year);

    // Shifts
    Task<ShiftsResponse> GetShiftsAsync();
    Task<ShiftResponse> GetEmployeeShiftAsync(string employeeId);

    // Overtime
    Task<OvertimeRequestResponse> CreateOvertimeRequestAsync(CreateOvertimeRequest request);
    Task<OvertimeRequestsResponse> GetOvertimeRequestsAsync(GetOvertimeRequestsRequest request);
    Task<OvertimeRequestResponse> GetOvertimeRequestDetailAsync(string requestId);
    Task<OvertimeRequestResponse> ApproveOvertimeRequestAsync(ApproveOvertimeRequest request);
    Task<OvertimeRequestResponse> RejectOvertimeRequestAsync(RejectOvertimeRequest request);
}
```

---

## Luồng xử lý

### Request Flow

```
┌──────────────┐         ┌───────────────────────────────────────────────────┐
│   Frontend   │         │                  API Gateway                       │
│  (Next.js)   │         └───────────────────────────────────────────────────┘
└──────┬───────┘                              │
       │                                      │
       │  HTTP Request                        │
       │  + JWT Token                         │
       │                                      ▼
       │                        ┌─────────────────────────┐
       │───────────────────────>│    CORS Middleware      │
       │                        └────────────┬────────────┘
       │                                     │
       │                        ┌────────────▼────────────┐
       │                        │    JWT Validation       │
       │                        │    (Keycloak)           │
       │                        └────────────┬────────────┘
       │                                     │
       │                        ┌────────────▼────────────┐
       │                        │  Authorization Policy   │
       │                        │  (Role-based)           │
       │                        └────────────┬────────────┘
       │                                     │
       │                        ┌────────────▼────────────┐
       │                        │     Controller          │
       │                        │  (Business Logic)       │
       │                        └────────────┬────────────┘
       │                                     │
       │                    ┌────────────────┼────────────────┐
       │                    │                │                │
       │                    ▼                ▼                ▼
       │            ┌───────────────┐ ┌───────────────┐ ┌───────────────┐
       │            │Employee Service│ │ Time Service  │ │Notification   │
       │            │   (gRPC)       │ │   (gRPC)      │ │Service (HTTP) │
       │            └───────────────┘ └───────────────┘ └───────────────┘
       │                    │                │                │
       │◄───────────────────┴────────────────┴────────────────┘
       │
       │  HTTP Response
       │  (JSON)
       ▼
```

### Aggregation Example

```
Frontend: GET /api/employees/{id}/full-profile

API Gateway:
    │
    ├──gRPC──> Employee Service: GetEmployee(id)
    │                           └── Returns: Basic Info, Department, Team
    │
    ├──gRPC──> Time Service: GetAttendanceStatus(id)
    │                       └── Returns: Today's attendance
    │
    └──gRPC──> Time Service: GetLeaveBalance(id, year)
                            └── Returns: Leave balance

Response: Aggregated Employee Profile
{
  "employee": { ... },
  "attendance": { ... },
  "leaveBalance": { ... }
}
```

### System Architecture

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                              FRONTEND (Next.js)                              │
│                               http://localhost:3000                          │
└───────────────────────────────────┬──────────────────────────────────────────┘
                                    │
                                    │ REST / GraphQL / WebSocket
                                    ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                            API GATEWAY (.NET)                                │
│                            http://localhost:5000                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐ │
│  │ Controllers │  │  GraphQL    │  │  SignalR    │  │     Swagger         │ │
│  │   (REST)    │  │ (HotChoco)  │  │    Hub      │  │  /swagger           │ │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘  └─────────────────────┘ │
│         │                │                │                                  │
│         └────────────────┴────────────────┘                                  │
│                          │                                                   │
│              ┌───────────▼───────────┐                                       │
│              │   Keycloak Validator  │                                       │
│              │      (JWT Auth)       │                                       │
│              └───────────┬───────────┘                                       │
└──────────────────────────┼───────────────────────────────────────────────────┘
                           │
         ┌─────────────────┼─────────────────┬───────────────────┐
         │                 │                 │                   │
         ▼                 ▼                 ▼                   ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│Employee Service │ │  Time Service   │ │ Notification    │ │    Keycloak     │
│     (gRPC)      │ │     (gRPC)      │ │ Service (HTTP)  │ │   (Auth Server) │
│ localhost:5002  │ │ localhost:5004  │ │ localhost:5005  │ │ localhost:8080  │
└────────┬────────┘ └────────┬────────┘ └────────┬────────┘ └─────────────────┘
         │                   │                   │
         ▼                   ▼                   ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│postgres-employee│ │  postgres-time  │ │postgres-notif   │
│    Port: 5432   │ │   Port: 5433    │ │   Port: 5434    │
└─────────────────┘ └─────────────────┘ └─────────────────┘
```

---

## Cấu hình

### Environment Variables

| Variable | Mô tả | Giá trị mặc định |
|----------|-------|------------------|
| `ASPNETCORE_ENVIRONMENT` | Môi trường | Development |
| `ASPNETCORE_URLS` | URLs lắng nghe | http://+:8080 |
| `GrpcServices__EmployeeService` | Employee Service URL | http://employee-service:8081 |
| `GrpcServices__TimeService` | Time Service URL | http://time-service:8081 |
| `NotificationService__Url` | Notification Service URL | http://notification-service:8080 |
| `Keycloak__Authority` | Keycloak realm URL | http://keycloak:8080/realms/hrm |
| `Keycloak__Audience` | API audience | hrm-api |
| `Keycloak__ClientId` | Client ID | hrm-api |
| `Keycloak__ClientSecret` | Client secret | hrm-api-secret |
| `Keycloak__RequireHttps` | Yêu cầu HTTPS | false |
| `Cors__AllowedOrigins__0` | CORS origin 1 | http://localhost:3000 |
| `Cors__AllowedOrigins__1` | CORS origin 2 | http://127.0.0.1:3000 |

### appsettings.json

```json
{
  "GrpcServices": {
    "EmployeeService": "http://localhost:5002",
    "TimeService": "http://localhost:5004"
  },
  "NotificationService": {
    "Url": "http://localhost:5005"
  },
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/hrm",
    "Audience": "hrm-api",
    "ClientId": "hrm-api",
    "ClientSecret": "hrm-api-secret",
    "RequireHttps": false
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://127.0.0.1:3000"
    ]
  }
}
```

---

## Chạy ứng dụng

### Với Docker Compose (Khuyến nghị)

```bash
# Từ thư mục hrm-deployment
cd hrm-deployment

# Chạy toàn bộ hệ thống
docker compose up -d

# Hoặc chỉ chạy API Gateway + dependencies
docker compose up -d keycloak employee-service time-service notification-service api-gateway
```

### Local Development

```bash
# 1. Start dependencies
cd hrm-deployment
docker compose up -d keycloak employee-service time-service notification-service

# 2. Run API Gateway
cd ../hrm-ApiGateway
dotnet run --project src/API
```

### Docker Build

```bash
# Build image
docker build -t hrm-api-gateway .

# Run container
docker run -p 5000:8080 \
  -e GrpcServices__EmployeeService="http://host.docker.internal:5002" \
  -e GrpcServices__TimeService="http://host.docker.internal:5004" \
  -e Keycloak__Authority="http://host.docker.internal:8080/realms/hrm" \
  hrm-api-gateway
```

### Ports

| Port | Protocol | Mô tả |
|------|----------|-------|
| 8080 (external: 5000) | HTTP | REST + GraphQL + Swagger |

### URLs

| Path | Mô tả |
|------|-------|
| `/swagger` | Swagger API Documentation |
| `/graphql` | GraphQL Playground |
| `/health` | Health check endpoint |
| `/hubs/notification` | SignalR Hub |

### Health Check

```bash
curl http://localhost:5000/health
```

---

## Test Users (Keycloak)

| Username | Password | Roles |
|----------|----------|-------|
| admin@hrm.vn | admin | system_admin, employee |
| hr@hrm.vn | hr123 | hr_staff, employee |
| manager@hrm.vn | manager123 | manager, employee |
| employee@hrm.vn | employee123 | employee |

---

## Troubleshooting

### Lỗi gRPC Connection

```bash
# Test Employee Service
grpcurl -plaintext localhost:5002 grpc.health.v1.Health/Check

# Test Time Service
grpcurl -plaintext localhost:5004 grpc.health.v1.Health/Check
```

### Lỗi Keycloak Authentication

```bash
# Kiểm tra Keycloak
curl http://localhost:8080/realms/hrm/.well-known/openid-configuration

# Test login
curl -X POST http://localhost:8080/realms/hrm/protocol/openid-connect/token \
  -d "client_id=hrm-api" \
  -d "client_secret=hrm-api-secret" \
  -d "grant_type=password" \
  -d "username=admin@hrm.vn" \
  -d "password=admin"
```

### Lỗi CORS

```bash
# Kiểm tra CORS headers
curl -v -X OPTIONS http://localhost:5000/api/employees \
  -H "Origin: http://localhost:3000" \
  -H "Access-Control-Request-Method: GET"
```

---

© 2025 HRM System - Clean Architecture
