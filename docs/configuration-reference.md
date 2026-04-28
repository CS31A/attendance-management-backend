# Configuration Reference

This document provides comprehensive information about all configuration options available in the Attendance Management System.

## Configuration Sources

The application uses multiple configuration sources in the following priority order (highest to lowest):

1. **Command Line Arguments**
2. **Environment Variables**
3. **User Secrets** (Development only)
4. **appsettings.{Environment}.json**
5. **appsettings.json**
6. **.env file** (via DotNetEnv)

Note: While the application loads .env files via DotNetEnv, there is currently no .env.example file provided in the repository.

## Configuration Files

### appsettings.json
Base configuration file with default settings.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "AppSettings": {
    "Token": "",
    "Issuer": "",
    "Audience": ""
  },
  "CookieSettings": {
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "AllowedEmailDomains": [
    "gmail.com",
    "outlook.com",
    "yahoo.com",
    "hotmail.com",
    "aol.com",
    "icloud.com",
    "protonmail.com",
    "yandex.com",
    "mail.com"
  ],
  "AllowedHosts": "*",
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://*:8080"
      },
      "Https": {
        "Url": "https://*:8081"
      }
    }
  }
}
```

### appsettings.Development.json
Development-specific overrides.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AttendanceDB_Dev;Trusted_Connection=true;TrustServerCertificate=true;"
  },
  "DetailedErrors": true,
  "AllowedHosts": "*"
}
```

### appsettings.Production.json
Production-specific configuration.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Error"
    }
  },
  "AllowedHosts": "yourdomain.com,www.yourdomain.com"
}
```

### .env File
Environment variables for sensitive configuration.

```env
# Database Configuration
ConnectionStrings__DefaultConnection=Server=localhost;Database=AttendanceDB;Trusted_Connection=true;TrustServerCertificate=true;

# JWT Configuration
AppSettings__Token=your-super-secret-jwt-key-here-make-it-at-least-32-characters-long
AppSettings__Issuer=AttendanceMonitoringAPI
AppSettings__Audience=AttendanceMonitoringUsers

# Cookie Settings
CookieSettings__AccessTokenExpirationMinutes=15
CookieSettings__RefreshTokenExpirationDays=7

# CORS Origins
AllowedOrigins__0=http://localhost:3000
AllowedOrigins__1=http://localhost:5173
AllowedOrigins__2=https://yourdomain.com
```

## Configuration Sections

### Database Configuration

#### ConnectionStrings
Database connection configuration.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "connection_string_here"
  }
}
```

**Connection String Formats:**

**Windows Authentication:**
```
Server=localhost;Database=AttendanceDB;Trusted_Connection=true;TrustServerCertificate=true;
```

**SQL Server Authentication:**
```
Server=localhost;Database=AttendanceDB;User Id=sa;Password=YourPassword;TrustServerCertificate=true;
```

**Azure SQL Database:**
```
Server=tcp:yourserver.database.windows.net,1433;Initial Catalog=AttendanceDB;Persist Security Info=False;User ID=yourusername;Password=yourpassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

**Environment Variable Override:**
```env
ConnectionStrings__DefaultConnection=your_connection_string_here
```

### JWT Authentication Configuration

#### AppSettings
JWT token configuration for authentication.

```json
{
  "AppSettings": {
    "Token": "your-secret-key-minimum-32-characters",
    "Issuer": "AttendanceMonitoringAPI",
    "Audience": "AttendanceMonitoringUsers"
  }
}
```

**Configuration Options:**

| Setting | Description | Required | Default |
|---------|-------------|----------|---------|
| `Token` | Secret key for JWT signing (min 32 chars) | Yes | - |
| `Issuer` | JWT issuer claim | Yes | - |
| `Audience` | JWT audience claim | Yes | - |

**Environment Variables:**
```env
AppSettings__Token=your-secret-key
AppSettings__Issuer=AttendanceMonitoringAPI
AppSettings__Audience=AttendanceMonitoringUsers
```

**Security Requirements:**
- Token must be at least 32 characters long
- Use cryptographically secure random generation
- Keep secret in production environments
- Rotate keys periodically

### Cookie Configuration

#### CookieSettings
HTTP-only cookie configuration for web authentication.

```json
{
  "CookieSettings": {
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

**Configuration Options:**

| Setting | Description | Type | Default | Range |
|---------|-------------|------|---------|-------|
| `AccessTokenExpirationMinutes` | Access token lifetime | int | 15 | 5-60 |
| `RefreshTokenExpirationDays` | Refresh token lifetime | int | 7 | 1-30 |

**Environment Variables:**
```env
CookieSettings__AccessTokenExpirationMinutes=15
CookieSettings__RefreshTokenExpirationDays=7
```

### Email Domain Validation

#### AllowedEmailDomains
Whitelist of allowed email domains for registration.

```json
{
  "AllowedEmailDomains": [
    "gmail.com",
    "outlook.com",
    "yahoo.com",
    "company.com"
  ]
}
```

**Environment Variables:**
```env
AllowedEmailDomains__0=gmail.com
AllowedEmailDomains__1=outlook.com
AllowedEmailDomains__2=company.com
```

### Logging Configuration

#### Logging
Structured logging configuration.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information",
      "System": "Warning",
      "Microsoft": "Warning"
    }
  }
}
```

**Log Levels:**
- **Trace**: Most detailed logs
- **Debug**: Debug information
- **Information**: General information
- **Warning**: Warning messages
- **Error**: Error messages
- **Critical**: Critical failures
- **None**: No logging

**Environment-Specific Logging:**

**Development:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

**Production:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.EntityFrameworkCore": "Error"
    }
  }
}
```

### Server Configuration

#### Kestrel
Web server configuration.

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://*:8080"
      },
      "Https": {
        "Url": "https://*:8081"
      }
    },
    "Limits": {
      "MaxConcurrentConnections": 100,
      "MaxConcurrentUpgradedConnections": 100,
      "MaxRequestBodySize": 30000000,
      "KeepAliveTimeout": "00:02:00",
      "RequestHeadersTimeout": "00:00:30"
    }
  }
}
```

**Configuration Options:**

| Setting | Description | Default |
|---------|-------------|---------|
| `Endpoints.Http.Url` | HTTP endpoint URL | http://*:8080 |
| `Endpoints.Https.Url` | HTTPS endpoint URL | https://*:8081 |
| `Limits.MaxConcurrentConnections` | Max concurrent connections | 100 |
| `Limits.MaxRequestBodySize` | Max request body size (bytes) | 30MB |
| `Limits.KeepAliveTimeout` | Keep-alive timeout | 2 minutes |

### CORS Configuration

#### AllowedOrigins
Cross-Origin Resource Sharing configuration.

```json
{
  "AllowedOrigins": [
    "http://localhost:3000",
    "http://localhost:5173",
    "https://yourdomain.com"
  ]
}
```

**Environment Variables:**
```env
AllowedOrigins__0=http://localhost:3000
AllowedOrigins__1=https://yourdomain.com
```

**Programmatic Configuration:**
```csharp
// In Program.cs or ServiceCollectionExtensions
services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", builder =>
    {
        builder.WithOrigins(configuration.GetSection("AllowedOrigins").Get<string[]>())
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});
```

## Environment-Specific Configuration

### Development Environment

**Characteristics:**
- Detailed logging enabled
- Development database
- Relaxed security settings
- Hot reload enabled
- Detailed error pages

**Key Settings:**
```json
{
  "Environment": "Development",
  "DetailedErrors": true,
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

### Staging Environment

**Characteristics:**
- Production-like configuration
- Staging database
- Moderate logging
- Performance monitoring

**Key Settings:**
```json
{
  "Environment": "Staging",
  "DetailedErrors": false,
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

### Production Environment

**Characteristics:**
- Minimal logging
- Production database
- Enhanced security
- Performance optimized
- Error handling

**Key Settings:**
```json
{
  "Environment": "Production",
  "DetailedErrors": false,
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.EntityFrameworkCore": "Error"
    }
  }
}
```

## Security Configuration

### JWT Security Best Practices

#### Token Configuration
```json
{
  "AppSettings": {
    "Token": "use-a-cryptographically-secure-random-key-at-least-32-characters-long",
    "TokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7,
    "RequireHttpsMetadata": true,
    "ValidateIssuer": true,
    "ValidateAudience": true,
    "ValidateLifetime": true,
    "ValidateIssuerSigningKey": true
  }
}
```

#### Security Headers
```json
{
  "SecurityHeaders": {
    "EnableHSTS": true,
    "HSTSMaxAge": 31536000,
    "EnableCSP": true,
    "CSPDirectives": "default-src 'self'; script-src 'self' 'unsafe-inline'",
    "EnableXFrameOptions": true,
    "XFrameOptions": "DENY"
  }
}
```

### Data Protection

#### Data Protection Configuration
```json
{
  "DataProtection": {
    "ApplicationName": "AttendanceManagement",
    "KeyLifetime": 90,
    "KeyRingPath": "/var/keys"
  }
}
```

## Performance Configuration

### Response Compression

#### Compression Settings
```json
{
  "ResponseCompression": {
    "EnableForHttps": true,
    "Providers": ["Gzip", "Brotli"],
    "MimeTypes": [
      "text/plain",
      "text/css",
      "application/javascript",
      "text/html",
      "application/xml",
      "text/xml",
      "application/json",
      "text/json"
    ]
  }
}
```

### Caching Configuration

#### Memory Cache Settings
```json
{
  "MemoryCache": {
    "SizeLimit": 100,
    "CompactionPercentage": 0.25,
    "ExpirationScanFrequency": "00:05:00"
  }
}
```

#### Response Caching
```json
{
  "ResponseCaching": {
    "MaximumBodySize": 1024,
    "UseCaseSensitivePaths": false,
    "VaryByHeader": "User-Agent"
  }
}
```

## Background Services Configuration

### Token Cleanup Service

#### Cleanup Settings
```json
{
  "BackgroundServices": {
    "TokenCleanup": {
      "Enabled": true,
      "IntervalMinutes": 60,
      "BatchSize": 1000,
      "RetentionDays": 30
    }
  }
}
```

### Health Checks Configuration

#### Health Check Settings
```json
{
  "HealthChecks": {
    "Enabled": true,
    "TimeoutSeconds": 30,
    "Checks": {
      "Database": {
        "Enabled": true,
        "TimeoutSeconds": 15
      },
      "Memory": {
        "Enabled": true,
        "ThresholdMB": 1024
      }
    }
  }
}
```

## Monitoring and Observability

### Application Insights (Optional)

#### Application Insights Configuration
```json
{
  "ApplicationInsights": {
    "InstrumentationKey": "your-instrumentation-key",
    "EnableAdaptiveSampling": true,
    "EnableQuickPulseMetricStream": true,
    "EnableDependencyTracking": true
  }
}
```

### Custom Metrics

#### Metrics Configuration
```json
{
  "Metrics": {
    "Enabled": true,
    "CollectionInterval": "00:01:00",
    "CustomCounters": [
      "attendance_records_created",
      "qr_codes_generated",
      "sessions_started"
    ]
  }
}
```

## Configuration Validation

### Startup Validation

The application validates critical configuration on startup:

```csharp
// Example validation in Program.cs
var jwtToken = builder.Configuration["AppSettings:Token"];
if (string.IsNullOrEmpty(jwtToken) || jwtToken.Length < 32)
{
    throw new InvalidOperationException("JWT Token must be at least 32 characters long");
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Database connection string is required");
}
```

### Configuration Testing

#### Test Configuration Values
```bash
# Test configuration loading
dotnet run --environment Development --urls "https://localhost:5001"

# Validate specific configuration
dotnet run -- --configuration:test
```

## Troubleshooting Configuration

### Common Configuration Issues

#### Missing Environment Variables
```bash
# Check if environment variables are loaded
echo $ConnectionStrings__DefaultConnection

# Windows
echo %ConnectionStrings__DefaultConnection%
```

#### Configuration Override Order
1. Command line arguments (highest priority)
2. Environment variables
3. User secrets (development)
4. appsettings.{Environment}.json
5. appsettings.json
6. .env file (lowest priority)

#### Debug Configuration Loading
```csharp
// Add to Program.cs for debugging
var config = builder.Configuration as IConfigurationRoot;
foreach (var provider in config.Providers)
{
    Console.WriteLine($"Provider: {provider.GetType().Name}");
}
```

### Configuration Best Practices

1. **Never commit secrets** to source control
2. **Use environment variables** for sensitive data
3. **Validate configuration** on startup
4. **Use different configurations** per environment
5. **Document all configuration options**
6. **Use strong typing** for configuration sections
7. **Implement configuration change detection**

### Environment Variable Naming

Follow the double underscore convention for nested configuration:

```env
# JSON: { "AppSettings": { "Token": "value" } }
AppSettings__Token=value

# JSON: { "ConnectionStrings": { "DefaultConnection": "value" } }
ConnectionStrings__DefaultConnection=value

# JSON: { "Logging": { "LogLevel": { "Default": "Information" } } }
Logging__LogLevel__Default=Information
```

This configuration reference provides comprehensive coverage of all available configuration options in the Attendance Management System.