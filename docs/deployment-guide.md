# Deployment Guide

This guide covers deploying the Attendance Management System to various environments.

## Prerequisites

- .NET 10.0 Runtime
- SQL Server (any edition)
- SSL Certificate (for HTTPS)

## Docker Deployment

### Using Docker

#### 1. Build the Image
```bash
cd attendance_monitoring
docker build -t attendance-api .
```

#### 2. Run with Docker
```bash
docker run -d \
  --name attendance-api \
  -p 8080:8080 \
  -p 8081:8081 \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;Database=AttendanceDB;User=sa;Password=YourPassword;TrustServerCertificate=true;" \
  -e AppSettings__Token="your-secret-token-at-least-32-characters-long" \
  -e AppSettings__Issuer="AttendanceAPI" \
  -e AppSettings__Audience="AttendanceUsers" \
  attendance-api
```

### Docker Compose

```yaml
# docker-compose.yml
version: '3.8'
services:
  api:
    build: ./attendance_monitoring
    ports:
      - "8080:8080"
      - "8081:8081"
    environment:
      - ConnectionStrings__DefaultConnection=Server=db;Database=AttendanceDB;User=sa;Password=YourStrong@Password;TrustServerCertificate=true;
      - AppSettings__Token=${JWT_TOKEN}
      - AppSettings__Issuer=AttendanceAPI
      - AppSettings__Audience=AttendanceUsers
      - CorsSettings__AllowedOrigins=https://yourdomain.com
    depends_on:
      - db
    restart: unless-stopped

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong@Password
    volumes:
      - sqldata:/var/opt/mssql
    restart: unless-stopped

volumes:
  sqldata:
```

## IIS Deployment

### 1. Prepare the Application
```bash
# Publish for deployment
dotnet publish -c Release -o ./publish
```

### 2. Install Prerequisites
- Install [.NET 10.0 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/10.0)
- Ensure IIS is installed with ASP.NET Core Module

### 3. Configure IIS
1. Create a new Application Pool with ".NET CLR Version" set to "No Managed Code"
2. Create a new website pointing to the `publish` folder
3. Set Application Pool identity with database access permissions

### 4. Configure web.config
The publish process generates `web.config`. Ensure environment variables are set:

```xml
<aspNetCore processPath="dotnet" arguments=".\attendance_monitoring.dll">
  <environmentVariables>
    <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
    <environmentVariable name="ConnectionStrings__DefaultConnection" value="..." />
    <environmentVariable name="AppSettings__Token" value="..." />
  </environmentVariables>
</aspNetCore>
```

## Azure App Service

### 1. Create Resources
```bash
# Create resource group
az group create --name attendance-rg --location eastus

# Create App Service plan
az appservice plan create \
  --name attendance-plan \
  --resource-group attendance-rg \
  --sku B1 \
  --is-linux

# Create Web App
az webapp create \
  --name attendance-api \
  --resource-group attendance-rg \
  --plan attendance-plan \
  --runtime "DOTNET|10.0"
```

### 2. Configure App Settings
```bash
az webapp config appsettings set \
  --name attendance-api \
  --resource-group attendance-rg \
  --settings \
    ConnectionStrings__DefaultConnection="..." \
    AppSettings__Token="..." \
    AppSettings__Issuer="AttendanceAPI" \
    AppSettings__Audience="AttendanceUsers"
```

### 3. Deploy
```bash
# Using ZIP deploy
dotnet publish -c Release -o ./publish
cd publish && zip -r ../deploy.zip .
az webapp deployment source config-zip \
  --name attendance-api \
  --resource-group attendance-rg \
  --src ../deploy.zip
```

## Linux Server (systemd)

### 1. Publish Application
```bash
dotnet publish -c Release -o /var/www/attendance-api
```

### 2. Create Service File
```ini
# /etc/systemd/system/attendance-api.service
[Unit]
Description=Attendance Management API
After=network.target

[Service]
WorkingDirectory=/var/www/attendance-api
ExecStart=/usr/bin/dotnet /var/www/attendance-api/attendance_monitoring.dll
Restart=always
RestartSec=10
SyslogIdentifier=attendance-api
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ConnectionStrings__DefaultConnection=...
Environment=AppSettings__Token=...

[Install]
WantedBy=multi-user.target
```

### 3. Enable and Start
```bash
sudo systemctl daemon-reload
sudo systemctl enable attendance-api
sudo systemctl start attendance-api
```

## Production Configuration

### Required Environment Variables
| Variable | Description |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `AppSettings__Token` | JWT secret key (min 32 chars) |
| `AppSettings__Issuer` | JWT issuer |
| `AppSettings__Audience` | JWT audience |
| `CorsSettings__AllowedOrigins` | Frontend URLs (semicolon-separated) |

### SSL/HTTPS Configuration
For production, always use HTTPS. Configure SSL certificates through:
- **IIS**: Bind SSL certificate in Site Bindings
- **Docker**: Use reverse proxy (nginx) or mount certificates
- **Azure**: Managed certificates or custom SSL

### Database Migration
The application applies pending EF Core migrations during startup before it begins serving requests. Production deployments should still use a connection string/account with permission to apply schema changes so cold starts can reach a healthy state automatically.

For controlled rollouts, you can still run the migration step ahead of time to shorten startup work or separate schema application from process restarts:
```bash
dotnet ef database update --connection "your-production-connection-string"
```

## Health Checks

Monitor application health:
- **Endpoint**: `GET /api/health`
- **Response**: Application and database status

## Troubleshooting

### Common Issues

1. **502 Bad Gateway**: Check if application started successfully
2. **Database connection errors**: Verify connection string and network access
3. **CORS errors**: Ensure frontend domain is in `CorsSettings__AllowedOrigins`

### Viewing Logs
```bash
# Docker
docker logs attendance-api

# systemd
journalctl -u attendance-api -f

# Application logs
tail -f /var/www/attendance-api/logs/app.log
```
