# Use the official .NET SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

# Copy everything and restore as distinct layers
COPY . ./
RUN dotnet restore

# Build the application in release mode
RUN dotnet publish -c Release -o out

# Use the official ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app

# Set environment variables for ASP.NET Core
ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_USE_POLLING_FILE_WATCHER=true

# Expose ports (matching the ports in launchsettings.json)
EXPOSE 7115
EXPOSE 5292

# Copy the published application from the build stage
COPY --from=build-env /app/out .

# Set the application URLs (as defined in launchSettings.json)
ENV ASPNETCORE_URLS="http://+:5292"

# Set the entry point to run the application
ENTRYPOINT ["dotnet", "MonitoringDashboardBackend.dll"]
