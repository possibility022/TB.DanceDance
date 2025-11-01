FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["backend/TB.DanceDance.API/TB.DanceDance.API.csproj", "backend/TB.DanceDance.API/"]
COPY ["backend/Application/Application.csproj", "backend/Application/"]
COPY ["backend/Domain/Domain.csproj", "backend/Domain/"]
COPY ["backend/Infrastructure/Infrastructure.csproj", "backend/Infrastructure/"]
COPY ["backend/TB.DanceDance.API.Contracts/TB.DanceDance.API.Contracts.csproj", "backend/TB.DanceDance.API.Contracts/"]
RUN dotnet restore "backend/TB.DanceDance.API/TB.DanceDance.API.csproj"
COPY . .
WORKDIR "/src/backend/TB.DanceDance.API"
RUN dotnet build "./TB.DanceDance.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TB.DanceDance.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys \
    && chown -R $APP_UID:$APP_UID /home/app/.aspnet

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TB.DanceDance.API.dll"]
