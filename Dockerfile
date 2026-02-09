# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for restore (no solution file needed)
COPY src/FlightTracker.Core/FlightTracker.Core.csproj src/FlightTracker.Core/
COPY src/FlightTracker.Data/FlightTracker.Data.csproj src/FlightTracker.Data/
COPY src/FlightTracker.Providers/FlightTracker.Providers.csproj src/FlightTracker.Providers/
COPY src/FlightTracker.Web/FlightTracker.Web.csproj src/FlightTracker.Web/

# Restore dependencies for Web project (includes all dependencies)
WORKDIR /src/src/FlightTracker.Web
RUN dotnet restore

# Copy source code
WORKDIR /src
COPY src/ src/

# Build application
WORKDIR /src/src/FlightTracker.Web
RUN dotnet build -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create data directory for SQLite database
RUN mkdir -p /app/data

# Copy published application
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__FlightTracker="Data Source=/app/data/flighttracker.db"

# Expose port
EXPOSE 8080

# Run the application
ENTRYPOINT ["dotnet", "FlightTracker.Web.dll"]
