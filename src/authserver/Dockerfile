FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["authserver/TB.Auth.Web.csproj", "authserver/"]
COPY ["backend/TB.DanceDance.API.Contracts/TB.DanceDance.API.Contracts.csproj", "backend/TB.DanceDance.API.Contracts/"]
RUN dotnet restore "authserver/TB.Auth.Web.csproj"
COPY . .
WORKDIR "/src/authserver"
RUN dotnet build "./TB.Auth.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TB.Auth.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys \
    && chown -R $APP_UID:$APP_UID /home/app/.aspnet
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TB.Auth.Web.dll"]
