FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PyroCache.Client/PyroCache.Client.csproj", "PyroCache.Client/"]
RUN dotnet restore "PyroCache.Client/PyroCache.Client.csproj"
COPY . .
WORKDIR "/src/PyroCache.Client"
RUN dotnet build "PyroCache.Client.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PyroCache.Client.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PyroCache.Client.dll"]
