FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5002
EXPOSE 5003

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/ToolsSharing.API/ToolsSharing.API.csproj", "src/ToolsSharing.API/"]
COPY ["src/ToolsSharing.Core/ToolsSharing.Core.csproj", "src/ToolsSharing.Core/"]
COPY ["src/ToolsSharing.Infrastructure/ToolsSharing.Infrastructure.csproj", "src/ToolsSharing.Infrastructure/"]
RUN dotnet restore "src/ToolsSharing.API/ToolsSharing.API.csproj"
COPY . .
WORKDIR "/src/src/ToolsSharing.API"
RUN dotnet build "ToolsSharing.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ToolsSharing.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ToolsSharing.API.dll"]