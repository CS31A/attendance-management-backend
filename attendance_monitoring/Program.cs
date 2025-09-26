using attendance_monitoring.Data;
using attendance_monitoring.IServices;
using attendance_monitoring.IRepository;
using attendance_monitoring.Repositories;
using attendance_monitoring.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

// Load environment variables from .env file
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Attendance Monitoring API", Version = "v1" });
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter JWT Bearer token",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            }, new List<string>()
        }
    });
});
builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection"); // fallback to in-memory if null
if (string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase("AttendanceDb"));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
}

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["AppSettings:Issuer"],
        ValidAudience = builder.Configuration["AppSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["AppSettings:Token"]!))
    };
    
    // Add cookie authentication for web login
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // If the request is for a web endpoint and contains cookies, use the cookie token
            if (context.Request.Cookies.ContainsKey("accessToken"))
            {
                context.Token = context.Request.Cookies["accessToken"];
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            // Check if the token has been blacklisted
            var tokenValidationService = context.HttpContext.RequestServices.GetRequiredService<ITokenValidationService>();
            var jti = context.Principal?.Claims.FirstOrDefault(c => c.Type == "jti")?.Value;
            
            if (!string.IsNullOrEmpty(jti) && await tokenValidationService.IsTokenBlacklistedAsync(jti))
            {
                // Token has been blacklisted
                context.Fail("Token has been revoked");
            }
            
            await Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(Options =>
{
    Options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
    Options.AddPolicy("PrivilegedPolicy", policy => policy.RequireRole("Admin", "Teacher"));
    Options.AddPolicy("UserPolicy", policy => policy.RequireRole("Admin", "Teacher", "Student"));
});

// Register repositories
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IInstructorRepository, InstructorRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<ISectionRepository, SectionRepository>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();

// Register services
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IInstructorService, InstructorService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IUserFactory, attendance_monitoring.Classes.Factory.UserFactory>();
builder.Services.AddScoped<ISectionService, SectionService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IRoleInitializationService, RoleInitializationService>();
builder.Services.AddScoped<UserContextService>();
builder.Services.AddScoped<ITokenValidationService, TokenValidationService>();

builder.Services.AddEndpointsApiExplorer();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Frontend origin
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // If you need to send cookies or authorization headers
    });
});

// Register background services
builder.Services.AddHostedService<BlacklistedTokenCleanupService>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddEventSourceLogger();

builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Attendance Monitoring API");
    });
    app.MapOpenApi();
    // Configure Scalar to use the Swagger-generated OpenAPI document (includes MVC controller endpoints)
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Attendance Monitoring API")
            .WithOpenApiRoutePattern("/swagger/v1/swagger.json");
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Initialize roles
using (var scope = app.Services.CreateScope())
{
    var roleInitializationService = scope.ServiceProvider.GetRequiredService<IRoleInitializationService>();
    roleInitializationService.InitializeRolesAsync().Wait();
}

app.Run();
