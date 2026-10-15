# Этап 1: Сборка
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["SRC/Products.API/Products.API.csproj", "SRC/Products.API/"]
RUN dotnet restore "SRC/Products.API/Products.API.csproj"
COPY . .
WORKDIR "/src/SRC/Products.API"
RUN dotnet publish "Products.API.csproj" -c Release -o /app/publish

# Этап 2: Рантайм
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Убираем HTTPS, так как в Docker у нас нет dev-сертификата
ENTRYPOINT ["dotnet", "Products.API.dll"]