FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sao chép các project file trước để tận dụng cache cho bước restore.
COPY ["Inventory.Api/Inventory.Api.csproj", "Inventory.Api/"]
COPY ["Inventory.Application/Inventory.Application.csproj", "Inventory.Application/"]
COPY ["Inventory.Domain/Inventory.Domain.csproj", "Inventory.Domain/"]
COPY ["Inventory.Infrastructure/Inventory.Infrastructure.csproj", "Inventory.Infrastructure/"]

RUN dotnet restore "Inventory.Api/Inventory.Api.csproj"

# Sao chép source code và publish ứng dụng.
COPY . .
WORKDIR "/src/Inventory.Api"
RUN dotnet publish "Inventory.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

USER app
ENTRYPOINT ["dotnet", "Inventory.Api.dll"]
