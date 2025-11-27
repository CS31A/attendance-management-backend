using attendance_monitoring.Extensions;
using attendance_monitoring.Extensions.ServiceCollectionExtensions;
using attendance_monitoring.Extensions.WebApplicationExtensions;

// Load environment variables from .env file
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// ===== SERVICE REGISTRATION =====

// Database & Identity
builder.Services.AddDatabaseServices(builder.Configuration);

// Authentication & Authorization
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddAuthorizationPolicies();

// API Documentation
builder.Services.AddApiDocumentation();

// Response Handling & CORS
builder.Services.AddResponseHandling();
builder.Services.AddCorsPolicy(builder.Configuration);

// SignalR
builder.Services.AddSignalRServices();

// Dependency Injection
builder.Services.AddRepositories();
builder.Services.AddApplicationServices();
builder.Services.AddBackgroundServices();

// Logging
builder.Logging.AddApplicationLogging();

// ===== BUILD APPLICATION =====

var app = builder.Build();

// ===== MIDDLEWARE PIPELINE =====

// Selective compression (must be first)
app.UseSelectiveResponseCompression();

// Performance monitoring
app.UsePerformanceMonitoring();

// Development tools (Swagger, Scalar)
app.UseDevelopmentTools();

// Static files for test client
app.UseStaticFiles();

// Core pipeline (HTTPS, CORS, Auth)
app.UseCorePipeline();

// Global exception handler
app.UseGlobalExceptionHandler();

// ===== STARTUP INITIALIZATION =====
await app.InitializeApplicationAsync();

// ===== RUN APPLICATION =====

await app.RunAsync();