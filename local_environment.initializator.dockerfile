#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base

USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Application", "Application/"]
COPY ["Domain", "Domain/"]
COPY ["Infrastructure", "Infrastructure/"]
WORKDIR "/src/Infrastructure"

FROM build AS publish
ARG BUILD_CONFIGURATION=Debug
ENV PATH $PATH:/root/.dotnet/tools
RUN dotnet tool install --global dotnet-ef
RUN dotnet build "./Infrastructure.csproj" -c $BUILD_CONFIGURATION
RUN dotnet-ef migrations script -o persistedGrant.sql --no-build --context PersistedGrantDbContext --idempotent
RUN dotnet-ef migrations script -o configuration.sql --no-build --context ConfigurationDbContext --idempotent
RUN dotnet-ef migrations script -o identityStore.sql --no-build --context IdentityStoreContext --idempotent
RUN dotnet-ef migrations script -o dance.sql --no-build --context DanceDbContext --idempotent

FROM base AS final

USER root
RUN apt-get update && apt-get install -y postgresql-client && rm -rf /var/lib/apt/lists/*
USER app

WORKDIR /app

COPY --from=publish /src/Infrastructure/*.sql .
COPY --chmod=755 tools/localsetup/InitializeEnvironment.sh .
COPY --chmod=755 tools/localsetup/set-identity-data.sql .

ENTRYPOINT ["./InitializeEnvironment.sh"]