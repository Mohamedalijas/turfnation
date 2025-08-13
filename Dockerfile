# Stage 1: Build with .NET 8 SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy all files and publish
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime with .NET 8 ASP.NET
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Expose port
EXPOSE 5000

# Start the app
# Make sure the DLL name matches your project file name (case-sensitive)
ENTRYPOINT ["dotnet", "turfnation.dll"]
