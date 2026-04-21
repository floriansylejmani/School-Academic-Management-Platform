FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["SchoolManagement.sln", "./"]
COPY ["src/SchoolManagement.API/SchoolManagement.API.csproj", "src/SchoolManagement.API/"]
COPY ["src/SchoolManagement.Application/SchoolManagement.Application.csproj", "src/SchoolManagement.Application/"]
COPY ["src/SchoolManagement.Domain/SchoolManagement.Domain.csproj", "src/SchoolManagement.Domain/"]
COPY ["src/SchoolManagement.Infrastructure/SchoolManagement.Infrastructure.csproj", "src/SchoolManagement.Infrastructure/"]
COPY ["src/SchoolManagement.Persistence/SchoolManagement.Persistence.csproj", "src/SchoolManagement.Persistence/"]

RUN dotnet restore "src/SchoolManagement.API/SchoolManagement.API.csproj"

COPY . .
RUN dotnet publish "src/SchoolManagement.API/SchoolManagement.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for the Docker healthcheck (aspnet:9.0 is Debian bookworm-slim
# and does not ship wget or curl by default).
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

# Create directories with proper permissions
RUN mkdir -p /app/wwwroot /home/app/.aspnet/DataProtection-Keys \
    && chown -R $APP_UID:0 /app/wwwroot /home/app/.aspnet \
    && chmod -R g=u /app/wwwroot /home/app/.aspnet

USER $APP_UID

ENTRYPOINT ["dotnet", "SchoolManagement.API.dll"]
