FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["VehicleSim.WebHost/VehicleSim.WebHost.csproj", "VehicleSim.WebHost/"]
COPY ["VehicleSim.Application/VehicleSim.Application.csproj", "VehicleSim.Application/"]
COPY ["VehicleSim.Core/VehicleSim.Core.csproj", "VehicleSim.Core/"]
COPY ["VehicleSim.Infrastructure/VehicleSim.Infrastructure.csproj", "VehicleSim.Infrastructure/"]
COPY ["VehicleSim.UI/VehicleSim.UI.csproj", "VehicleSim.UI/"]
RUN dotnet restore "./VehicleSim.WebHost/VehicleSim.WebHost.csproj"
COPY . .
WORKDIR "/src/VehicleSim.WebHost"
RUN dotnet build "./VehicleSim.WebHost.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM node:25-alpine AS front-end
WORKDIR /front-end
COPY VehicleSim.ClientApp ./VehicleSim.ClientApp
WORKDIR /front-end/VehicleSim.ClientApp
RUN npm install && npm run build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./VehicleSim.WebHost.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .
COPY --from=front-end /front-end/VehicleSim.ClientApp/dist ./wwwroot

ENTRYPOINT ["dotnet", "VehicleSim.WebHost.dll"]