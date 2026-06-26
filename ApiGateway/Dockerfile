FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ApiGateway/PRN232.LMSSystem.ApiGateway.csproj", "ApiGateway/"]
RUN dotnet restore "ApiGateway/PRN232.LMSSystem.ApiGateway.csproj"
COPY . .
WORKDIR "/src/ApiGateway"
RUN dotnet build "PRN232.LMSSystem.ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PRN232.LMSSystem.ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PRN232.LMSSystem.ApiGateway.dll"]
