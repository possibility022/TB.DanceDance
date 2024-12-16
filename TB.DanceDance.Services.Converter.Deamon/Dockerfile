#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base

RUN apt-get update -qq && apt-get install ffmpeg libgdiplus -y

USER app
WORKDIR /app

RUN mkdir ./mediafolder
ENV tb.dancedance.converter.workdir='/app/mediafolder'

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TB.DanceDance.Services.Converter.Deamon/TB.DanceDance.Services.Converter.Deamon.csproj", "TB.DanceDance.Services.Converter.Deamon/"]
COPY ["TB.DanceDance.API.Contracts/TB.DanceDance.API.Contracts.csproj", "TB.DanceDance.API.Contracts/"]
RUN dotnet restore "./TB.DanceDance.Services.Converter.Deamon/TB.DanceDance.Services.Converter.Deamon.csproj"
COPY . .
WORKDIR "/src/TB.DanceDance.Services.Converter.Deamon"
RUN dotnet build "./TB.DanceDance.Services.Converter.Deamon.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TB.DanceDance.Services.Converter.Deamon.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TB.DanceDance.Services.Converter.Deamon.dll"]