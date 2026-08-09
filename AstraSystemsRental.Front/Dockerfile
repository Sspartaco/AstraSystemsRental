FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

RUN apt-get update && apt-get install -y --no-install-recommends curl gnupg \
    && curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y --no-install-recommends nodejs \
    && rm -rf /var/lib/apt/lists/*

COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY nuget.config .
COPY nuget-local/ nuget-local/
COPY AstraSystemsRental.Contracts/src/ AstraSystemsRental.Contracts/src/
COPY AstraSystemsRental.Front/src/ AstraSystemsRental.Front/src/

WORKDIR /src/AstraSystemsRental.Front/src/AstraSystemsRental.Front
RUN npm install && npm run build:css

WORKDIR /src
RUN dotnet restore AstraSystemsRental.Front/src/AstraSystemsRental.Front/AstraSystemsRental.Front.csproj \
    --source https://api.nuget.org/v3/index.json \
    --source /src/nuget-local
RUN dotnet publish AstraSystemsRental.Front/src/AstraSystemsRental.Front/AstraSystemsRental.Front.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/auth/login || exit 1

ENTRYPOINT ["dotnet", "AstraSystemsRental.Front.dll"]
