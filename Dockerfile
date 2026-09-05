FROM mcr.microsoft.com/dotnet/sdk:10.0.302 AS build

WORKDIR /src
COPY backend/src/SmartTaxi.Domain/SmartTaxi.Domain.csproj backend/src/SmartTaxi.Domain/
COPY backend/src/SmartTaxi.Application/SmartTaxi.Application.csproj backend/src/SmartTaxi.Application/
COPY backend/src/SmartTaxi.Infrastructure/SmartTaxi.Infrastructure.csproj backend/src/SmartTaxi.Infrastructure/
COPY backend/src/SmartTaxi.API/SmartTaxi.API.csproj backend/src/SmartTaxi.API/
RUN dotnet restore backend/src/SmartTaxi.API/SmartTaxi.API.csproj
COPY backend/ backend/
RUN dotnet publish backend/src/SmartTaxi.API/SmartTaxi.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartTaxi.API.dll"]

