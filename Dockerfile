# Use ASP.NET Core Runtime for the final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Use SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Foodtrucks.Api/Foodtrucks.Api.csproj", "src/Foodtrucks.Api/"]
RUN dotnet restore "src/Foodtrucks.Api/Foodtrucks.Api.csproj"
COPY . .
WORKDIR "/src/src/Foodtrucks.Api"
RUN dotnet build "Foodtrucks.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Foodtrucks.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Foodtrucks.Api.dll"]
