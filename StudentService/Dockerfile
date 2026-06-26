FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["StudentService/PRN232.LMSSystem.StudentService.csproj", "StudentService/"]
RUN dotnet restore "StudentService/PRN232.LMSSystem.StudentService.csproj"
COPY . .
WORKDIR "/src/StudentService"
RUN dotnet build "PRN232.LMSSystem.StudentService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PRN232.LMSSystem.StudentService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PRN232.LMSSystem.StudentService.dll"]
