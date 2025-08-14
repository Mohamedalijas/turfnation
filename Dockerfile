# Stage 1: Build with .NET 8 SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Install dotnet-env for development
RUN dotnet tool install --global dotnet-env

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy all files and publish
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime with .NET 8 ASP.NET
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Make port configurable via env
ENV PORT=5000
EXPOSE $PORT

# Health check
HEALTHCHECK --interval=30s --timeout=3s CMD curl -f http://localhost:$PORT/health || exit 1

# Start the app
ENTRYPOINT ["dotnet", "TurfAuthAPI.dll"]
