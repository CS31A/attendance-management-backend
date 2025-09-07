FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["attendance_monitoring/attendance_monitoring.csproj", "attendance_monitoring/"]
RUN dotnet restore "attendance_monitoring/attendance_monitoring.csproj"
COPY . .
WORKDIR "/src/attendance_monitoring"
RUN dotnet build "./attendance_monitoring.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./attendance_monitoring.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Set environment variable for HTTPS port
ENV ASPNETCORE_HTTPS_PORT=8081
ENTRYPOINT ["dotnet", "attendance_monitoring.dll"]
