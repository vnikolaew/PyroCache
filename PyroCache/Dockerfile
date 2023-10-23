FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["PyroCache.csproj", "./"]
RUN dotnet restore "PyroCache.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "PyroCache.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
COPY --from=publish /app/publish .
RUN dotnet publish "PyroCache.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
ENTRYPOINT ["dotnet", "PyroCache.dll"]
