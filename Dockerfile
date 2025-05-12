FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and project files
COPY *.sln .
COPY Chakal.Core/*.csproj ./Chakal.Core/
COPY Chakal.Application/*.csproj ./Chakal.Application/
COPY Chakal.Infrastructure/*.csproj ./Chakal.Infrastructure/
COPY Chakal.Infrastructure.Models/*.csproj ./Chakal.Infrastructure.Models/
COPY Chakal.IngestSystem/*.csproj ./Chakal.IngestSystem/

# Restore dependencies
RUN dotnet restore

# Copy the source code
COPY . .

# Build the application
RUN dotnet build -c Release --no-restore

# Publish the application
RUN dotnet publish -c Release -o /app/publish --no-build

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy SQL scripts for initialization
COPY sql/ /app/sql/

# Copy the published application
COPY --from=build /app/publish .

# Set the entry point
ENTRYPOINT ["dotnet", "Chakal.IngestSystem.dll"] 