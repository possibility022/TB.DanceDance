#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base

USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src"
COPY ["src/backend/Application", "Application/"]
COPY ["src/backend/Domain", "Domain/"]
COPY ["src/backend/TB.DanceDance.API.Contracts", "TB.DanceDance.API.Contracts/"]
COPY ["src/backend/Infrastructure", "Infrastructure/"]

WORKDIR "/src/Infrastructure"
FROM build AS infrastructure
ARG BUILD_CONFIGURATION=Debug
ENV PATH=$PATH:/root/.dotnet/tools
RUN dotnet tool install --global dotnet-ef
RUN dotnet build "./Infrastructure.csproj" -c $BUILD_CONFIGURATION
RUN dotnet-ef migrations script -o persistedGrant-migrations.sql --no-build --context PersistedGrantDbContext --idempotent
RUN dotnet-ef migrations script -o configuration-migrations.sql --no-build --context ConfigurationDbContext --idempotent
RUN dotnet-ef migrations script -o identityStore-migrations.sql --no-build --context IdentityStoreContext --idempotent
RUN dotnet-ef migrations script -o danceDb-migrations.sql --no-build --context DanceDbContext --idempotent

FROM build AS blobloader
WORKDIR "/src/BlobLoader"
COPY ["tools/localsetup/BlobLoader","."]
RUN dotnet build -c Release

FROM base AS final

USER root
RUN apt-get update && apt-get install -y postgresql-client && rm -rf /var/lib/apt/lists/*

USER app
WORKDIR /app

COPY --from=blobloader "/src/BlobLoader/bin/Release/net10.0/*" .
COPY --from=infrastructure /src/Infrastructure/*.sql .
COPY --chmod=755 tools/localsetup/InitializeEnvironment.sh .
COPY --chmod=744 'tools/localsetup/*-seed.sql' .

# Replace \r\n with \n in InitializeEnvironment.sh
RUN sed -i 's/\r$//' InitializeEnvironment.sh

ENTRYPOINT ["./InitializeEnvironment.sh"]