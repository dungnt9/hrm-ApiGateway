# API Gateway

This service acts as the main entry point for the HRM backend, exposing REST and GraphQL APIs, handling authentication, and proxying requests to internal microservices.

## Features

- REST API aggregation
- GraphQL endpoint for organization chart and employee queries
- Authentication and authorization (Keycloak)
- gRPC client to EmployeeService and TimeService
- Service health check endpoint

## Tech Stack

- .NET 8, ASP.NET Core Web API
- GraphQL (HotChocolate)
- gRPC client
- Docker

## Endpoints

- REST: `/api/employees`, `/api/attendance`, `/api/leaves`, etc.
- GraphQL: `/graphql`
- Health: `/health`

## Environment Variables

- `GrpcServices__EmployeeService`
- `GrpcServices__TimeService`
- `Keycloak__Authority`, `Keycloak__Audience`

## Running Locally

```sh
docker-compose up -d
ASPNETCORE_ENVIRONMENT=Development dotnet run --project ApiGateway
```

## Docker

Service is built and run via Docker Compose. See root `docker-compose.yml`.

## Notes

- Requires all backend services to be healthy before startup
- Handles authentication for all API requests

---

© 2025 HRM System
