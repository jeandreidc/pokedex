# syntax=docker/dockerfile:1

# =============================================================================
# API — shared .NET dependency restore
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dotnet-restore
WORKDIR /src

COPY ["src/Kota.Pokedex.Core/Kota.Pokedex.Core.csproj", "Kota.Pokedex.Core/"]
COPY ["src/Kota.Pokedex.Application/Kota.Pokedex.Application.csproj", "Kota.Pokedex.Application/"]
COPY ["src/Kota.Pokedex.Infrastructure/Kota.Pokedex.Infrastructure.csproj", "Kota.Pokedex.Infrastructure/"]
COPY ["src/Kota.Pokedex.ServiceDefaults/Kota.Pokedex.ServiceDefaults.csproj", "Kota.Pokedex.ServiceDefaults/"]
COPY ["src/Kota.Pokedex.Api/Kota.Pokedex.Api.csproj", "Kota.Pokedex.Api/"]

RUN dotnet restore "Kota.Pokedex.Api/Kota.Pokedex.Api.csproj"

FROM dotnet-restore AS api-build
COPY src/Kota.Pokedex.Core/ Kota.Pokedex.Core/
COPY src/Kota.Pokedex.Application/ Kota.Pokedex.Application/
COPY src/Kota.Pokedex.Infrastructure/ Kota.Pokedex.Infrastructure/
COPY src/Kota.Pokedex.ServiceDefaults/ Kota.Pokedex.ServiceDefaults/
COPY src/Kota.Pokedex.Api/ Kota.Pokedex.Api/

RUN dotnet publish "Kota.Pokedex.Api/Kota.Pokedex.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=api-build /app/publish .
ENTRYPOINT ["dotnet", "Kota.Pokedex.Api.dll"]

# =============================================================================
# Web — Angular build + nginx
# =============================================================================
FROM node:22-alpine AS web-build
WORKDIR /app

COPY src/web/package*.json ./
RUN npm ci

COPY src/web/ ./
RUN npm run build -- --configuration production

FROM nginx:alpine AS web
COPY infra/docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=web-build /app/dist/web/browser /usr/share/nginx/html
EXPOSE 80
