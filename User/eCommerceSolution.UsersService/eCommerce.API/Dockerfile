# UsersService — multi-stage build (API + Core + Infrastructure)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["eCommerce.API/eCommerce.API.csproj", "eCommerce.API/"]
COPY ["eCommerce.Core/eCommerce.Core.csproj", "eCommerce.Core/"]
COPY ["eCommerce.Infrastructure/eCommerce.Infrastructure.csproj", "eCommerce.Infrastructure/"]
RUN dotnet restore "eCommerce.API/eCommerce.API.csproj"

COPY . .
WORKDIR /src/eCommerce.API
RUN dotnet publish "eCommerce.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "eCommerce.API.dll"]
