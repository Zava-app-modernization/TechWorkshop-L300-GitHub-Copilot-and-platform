# =============================================================================
# ZavaStorefront Dockerfile
# Multi-stage build for .NET 6.0 ASP.NET Core Web Application
# =============================================================================

# -----------------------------------------------------------------------------
# Stage 1: Build
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY src/ZavaStorefront.csproj ./
RUN dotnet restore

# Copy the rest of the source code
COPY src/ ./

# Build the application
RUN dotnet build -c Release -o /app/build

# -----------------------------------------------------------------------------
# Stage 2: Publish
# -----------------------------------------------------------------------------
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# -----------------------------------------------------------------------------
# Stage 3: Runtime
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app

# Expose port 80 for HTTP traffic
EXPOSE 80
EXPOSE 443

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# Copy published application
COPY --from=publish /app/publish .

# Set the entry point
ENTRYPOINT ["dotnet", "ZavaStorefront.dll"]
