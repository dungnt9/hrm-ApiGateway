using ApiGateway.Configuration;
using ApiGateway.GraphQL;
using ApiGateway.Hubs;
using ApiGateway.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "HRM API Gateway", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience = builder.Configuration["Keycloak:Audience"];
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Employee", policy => policy.RequireRole("employee"));
    options.AddPolicy("Manager", policy => policy.RequireRole("manager"));
    options.AddPolicy("HRStaff", policy => policy.RequireRole("hr_staff"));
    options.AddPolicy("Admin", policy => policy.RequireRole("system_admin"));
    options.AddPolicy("ManagerOrHR", policy => policy.RequireRole("manager", "hr_staff"));
});

// Configure gRPC clients
builder.Services.Configure<GrpcServicesConfig>(builder.Configuration.GetSection("GrpcServices"));

builder.Services.AddGrpcClient<ApiGateway.Protos.EmployeeGrpc.EmployeeGrpcClient>("EmployeeService", o =>
{
    o.Address = new Uri(builder.Configuration["GrpcServices:EmployeeService"] ?? "http://localhost:5002");
});

builder.Services.AddGrpcClient<ApiGateway.Protos.TimeGrpc.TimeGrpcClient>("TimeService", o =>
{
    o.Address = new Uri(builder.Configuration["GrpcServices:TimeService"] ?? "http://localhost:5004");
});

// Register services
builder.Services.AddScoped<IEmployeeGrpcService, EmployeeGrpcService>();
builder.Services.AddScoped<ITimeGrpcService, TimeGrpcService>();

// Configure SignalR
builder.Services.AddSignalR();

// Configure FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Configure GraphQL (HotChocolate)
builder.Services
    .AddGraphQLServer()
    .AddQueryType<OrgChartQuery>()
    .AddAuthorization()
    .AddFiltering()
    .AddSorting();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://127.0.0.1:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// HttpClient for Keycloak
builder.Services.AddHttpClient();

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGraphQL();
app.MapHealthChecks("/health");
app.MapHub<NotificationHub>("/hubs/notification");

app.Run();
