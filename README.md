# API Gateway

This service acts as the main entry point for the HRM backend, exposing REST and GraphQL APIs, handling authentication, and proxying requests to internal microservices.

## Features

- **REST API aggregation** - Unified endpoint for all backend operations
- **GraphQL endpoint** for organization chart and employee queries
- **Authentication & Authorization** (Keycloak JWT with RBAC)
- **gRPC client** to Employee Service and Time Service
- **HTTP proxy** to Notification Service
- **Real-time notifications** via SignalR hub
- **Service health check** endpoint
- **Swagger documentation** for REST APIs

## Tech Stack

- .NET 8, ASP.NET Core Web API
- GraphQL (HotChocolate)
- gRPC client
- SignalR
- JWT Bearer Authentication
- FluentValidation
- Serilog logging
- Docker

## REST API Endpoints

### Authentication (`/api/auth`)
- `POST /login` - Login with username/password
- `POST /refresh` - Refresh JWT token
- `POST /logout` - Logout (invalidate token)
- `GET /me` - Get current user info

### Employees (`/api/employees`)
- `GET /` - List employees (pagination, filter by department/team/search)
- `GET /{id}` - Get employee details
- `POST /` - Create employee (HR Staff)
- `PUT /{id}` - Update employee (HR Staff)
- `DELETE /{id}` - Delete employee (Admin)
- `GET /{id}/manager` - Get employee's manager
- `GET /team/{teamId}` - Get team members (Manager/HR)
- `GET /manager/{managerId}/team` - Get manager's team (Manager/HR)
- `POST /{id}/assign-role` - Assign role to employee (Admin)
- `GET /departments` - List departments
- `GET /teams` - List teams

### Attendance (`/api/attendance`)
- `POST /check-in` - Employee check-in (with GPS location)
- `POST /check-out` - Employee check-out (with GPS location)
- `GET /status` - Get current attendance status
- `GET /history` - Get attendance history (pagination, date range)
- `GET /team/{teamId}` - Get team attendance for a date (Manager/HR)
- `GET /shifts` - List all shifts
- `GET /shift` - Get employee's current shift

### Leave Management (`/api/leaves`)
- `POST /request` - Create leave request
- `GET /requests` - Get user's leave requests (pagination)
- `GET /requests/pending` - Get pending approvals (Manager/HR)
- `GET /request/{id}` - Get leave request details
- `POST /request/{id}/approve` - Approve leave (Manager/HR)
- `POST /request/{id}/reject` - Reject leave (Manager/HR)
- `GET /balance` - Get leave balance for user

### Overtime (`/api/overtime`)
- `POST /request` - Create overtime request
- `GET /requests` - Get overtime requests (pagination, filter)
- `GET /requests/pending` - Get pending overtime approvals (Manager/HR)
- `GET /request/{id}` - Get overtime request details
- `POST /request/{id}/approve` - Approve overtime (Manager/HR)
- `POST /request/{id}/reject` - Reject overtime (Manager/HR)

### Notifications (`/api/notifications`)
- `GET /` - Get notifications (pagination, unreadOnly filter)
- `POST /{id}/read` - Mark notification as read
- `POST /read-all` - Mark all notifications as read
- `GET /templates` - Get notification templates (Admin)
- `GET /preferences` - Get user notification preferences
- `PUT /preferences` - Update notification preferences

### GraphQL (`/graphql`)
- `query getOrgChart(rootId, depth)` - Get organization chart
- `query getDepartments(companyId)` - Get departments
- `query getTeams(departmentId)` - Get teams
- `query getTeamMembers(teamId, managerId)` - Get team members

### Health (`/health`)
- `GET /health` - Health check endpoint

## Database Schema

Not directly (proxies to gRPC services), but manages:
- JWT token validation
- Request/response transformation
- Error handling and logging

## Environment Variables

- `GrpcServices__EmployeeService` - Employee Service gRPC endpoint (default: http://localhost:5002)
- `GrpcServices__TimeService` - Time Service gRPC endpoint (default: http://localhost:5004)
- `NotificationService__Url` - Notification Service HTTP endpoint (default: http://localhost:5005)
- `Keycloak__Authority` - Keycloak realm endpoint (default: http://localhost:8080/realms/hrm)
- `Keycloak__Audience` - Keycloak audience (default: hrm-api)
- `Keycloak__ClientId` - Keycloak client ID
- `ASPNETCORE_ENVIRONMENT` - Development/Production

## Authorization Policies

All REST endpoints require JWT Bearer token. Policies:
- `Employee` - Requires 'employee' role
- `Manager` - Requires 'manager' role
- `HRStaff` - Requires 'hr_staff' role
- `Admin` - Requires 'system_admin' role
- `ManagerOrHR` - Requires 'manager' OR 'hr_staff' role
- Default (no policy) - Requires authenticated user (any role)

## Running Locally

```sh
# Start all infrastructure
docker-compose up -d

# Run with .NET CLI (requires all dependencies)
ASPNETCORE_ENVIRONMENT=Development dotnet run --project ApiGateway

# Or build and run with Docker
docker-compose build api-gateway
docker-compose up api-gateway
```

## Docker

Service is built and run via Docker Compose. See root `docker-compose.yml`.

Port mapping:
- Container: 8080 (HTTP), 8081 (HTTPS)
- Host: 5000 (HTTP)

## Architecture

```
Frontend (port 3000)
    ↓ REST/GraphQL/WebSocket
API Gateway (port 5000)
    ├─→ gRPC → Employee Service (port 5002)
    ├─→ gRPC → Time Service (port 5004)
    ├─→ HTTP → Notification Service (port 5005)
    └─→ SignalR Hub (/hubs/notification)
```

## Features in Detail

### 1. Request Aggregation
- Combines data from multiple gRPC services
- Single REST endpoint replaces multiple backend calls
- Built-in error handling and retry logic

### 2. Authentication
- JWT Bearer token validation
- Keycloak integration for user roles/permissions
- Token refresh support

### 3. Authorization
- Role-based access control (RBAC)
- Policy-based endpoint protection
- Claims-based authorization

### 4. Real-time Notifications
- SignalR hub for real-time communication
- Group-based messaging (by user ID)
- Auto-reconnect with exponential backoff
- JWT authentication for WebSocket connections

### 5. GraphQL Support
- Flexible querying for organization data
- Reduces over-fetching/under-fetching
- Supports filtering and sorting

### 6. Logging & Monitoring
- Serilog structured logging
- Request/response logging
- Performance metrics
- Health check endpoint

## Notes

- Requires all backend services to be healthy before startup
- Handles authentication for all API requests
- Proxies Notification Service endpoints (no direct access needed from frontend)
- SignalR hub provides unified WebSocket endpoint
- Team attendance aggregates data from Employee and Time services
- All list endpoints support pagination (page, pageSize)
- All filter parameters are optional

## Error Handling

Common HTTP status codes:
- `200 OK` - Success
- `201 Created` - Resource created
- `204 No Content` - Success (no response body)
- `400 Bad Request` - Invalid input
- `401 Unauthorized` - Missing/invalid token
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error

---

© 2025 HRM System
