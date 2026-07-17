# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
COPY ["src/Aurora.Flowboard.Api/Aurora.Flowboard.Api.csproj", "src/Aurora.Flowboard.Api/"]
COPY ["src/Aurora Flowboard.ServiceDefaults/Aurora Flowboard.ServiceDefaults.csproj", "src/Aurora Flowboard.ServiceDefaults/"]
COPY ["src/Aurora.Flowboard.Infrastructure/Aurora.Flowboard.Infrastructure.csproj", "src/Aurora.Flowboard.Infrastructure/"]
COPY ["src/Aurora.Flowboard.Application/Aurora.Flowboard.Application.csproj", "src/Aurora.Flowboard.Application/"]
COPY ["src/Aurora.Flowboard.Domain/Aurora.Flowboard.Domain.csproj", "src/Aurora.Flowboard.Domain/"]
RUN dotnet restore "./src/Aurora.Flowboard.Api/Aurora.Flowboard.Api.csproj"
COPY . .
WORKDIR "/src/src/Aurora.Flowboard.Api"
RUN dotnet build "./Aurora.Flowboard.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Aurora.Flowboard.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Aurora.Flowboard.Api.dll"]