FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base

USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
ENV PATH=$PATH:/root/.dotnet/tools
RUN dotnet tool install --global dotnet-ef

COPY ["src/backend", "backend/"]
COPY ["src/authserver", "authserver/"]

RUN mkdir -p /artifacts

RUN dotnet build "/src/backend/TB.DanceDance.Access/TB.DanceDance.Access.csproj" -c $BUILD_CONFIGURATION
RUN dotnet-ef migrations script \
    --project /src/backend/TB.DanceDance.Access/TB.DanceDance.Access.csproj \
    --startup-project /src/backend/TB.DanceDance.Access/TB.DanceDance.Access.csproj \
    --context AccessDbContext \
    --idempotent \
    --output /artifacts/access-migrations.sql

RUN dotnet build "/src/backend/TB.DanceDance.Videos/TB.DanceDance.Videos.csproj" -c $BUILD_CONFIGURATION
RUN dotnet-ef migrations script \
    --project /src/backend/TB.DanceDance.Videos/TB.DanceDance.Videos.csproj \
    --startup-project /src/backend/TB.DanceDance.Videos/TB.DanceDance.Videos.csproj \
    --context VideosDbContext \
    --idempotent \
    --output /artifacts/videos-migrations.sql

RUN dotnet build "/src/authserver/TB.Auth.Web.csproj" -c $BUILD_CONFIGURATION
RUN dotnet-ef migrations script \
    --project /src/authserver/TB.Auth.Web.csproj \
    --startup-project /src/authserver/TB.Auth.Web.csproj \
    --context TB.Auth.Web.Identity.IdentityStoreContext \
    --idempotent \
    --output /artifacts/auth-identity-migrations.sql
RUN dotnet-ef migrations script \
    --project /src/authserver/TB.Auth.Web.csproj \
    --startup-project /src/authserver/TB.Auth.Web.csproj \
    --context TB.Auth.Web.Identity.AuthStoreContext \
    --idempotent \
    --output /artifacts/auth-openiddict-migrations.sql

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS blobloader
WORKDIR /src/BlobLoader
COPY ["tools/localsetup/BlobLoader", "."]
RUN dotnet build -c Release

FROM base AS final

USER root
RUN apt-get update && apt-get install -y postgresql-client && rm -rf /var/lib/apt/lists/*

USER app
WORKDIR /app

COPY --from=blobloader "/src/BlobLoader/bin/Release/net10.0/*" .
COPY --from=build /artifacts/*.sql .
COPY --chmod=755 tools/localsetup/InitializeEnvironment.sh .
COPY --chmod=744 tools/localsetup/identity-data-seed.sql .
COPY --chmod=744 tools/localsetup/oauth-data-seed.sql .
COPY --chmod=744 tools/localsetup/dance-data-seed.sql .

# Replace \r\n with \n in InitializeEnvironment.sh
RUN sed -i 's/\r$//' InitializeEnvironment.sh

ENTRYPOINT ["./InitializeEnvironment.sh"]
