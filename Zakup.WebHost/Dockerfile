﻿FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Zakup.WebHost/Zakup.WebHost.csproj", "Zakup.WebHost/"]
RUN dotnet restore "Zakup.WebHost/Zakup.WebHost.csproj"
COPY . .
WORKDIR "/src/Zakup.WebHost"
RUN dotnet build "./Zakup.WebHost.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Zakup.WebHost.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Zakup.WebHost.dll"]
