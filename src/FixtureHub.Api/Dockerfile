FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /source

COPY global.json Directory.Build.props .editorconfig ./
COPY src/FixtureHub.Domain/FixtureHub.Domain.csproj src/FixtureHub.Domain/
COPY src/FixtureHub.Application/FixtureHub.Application.csproj src/FixtureHub.Application/
COPY src/FixtureHub.Infrastructure/FixtureHub.Infrastructure.csproj src/FixtureHub.Infrastructure/
COPY src/FixtureHub.Api/FixtureHub.Api.csproj src/FixtureHub.Api/
RUN dotnet restore src/FixtureHub.Api/FixtureHub.Api.csproj

COPY src/ src/
RUN dotnet publish src/FixtureHub.Api/FixtureHub.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app \
    -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.12 AS final

RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app .

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "FixtureHub.Api.dll"]
