FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CourseService/PRN232.LMSSystem.CourseService.csproj", "CourseService/"]
RUN dotnet restore "CourseService/PRN232.LMSSystem.CourseService.csproj"
COPY . .
WORKDIR "/src/CourseService"
RUN dotnet build "PRN232.LMSSystem.CourseService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PRN232.LMSSystem.CourseService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PRN232.LMSSystem.CourseService.dll"]
