FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Kota.Pokedex.Api/Kota.Pokedex.Api.csproj", "Kota.Pokedex.Api/"]
RUN dotnet restore "Kota.Pokedex.Api/Kota.Pokedex.Api.csproj"

COPY src/Kota.Pokedex.Api/ Kota.Pokedex.Api/
RUN dotnet publish "Kota.Pokedex.Api/Kota.Pokedex.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Kota.Pokedex.Api.dll"]
