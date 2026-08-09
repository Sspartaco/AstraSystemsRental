FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY nuget.config .
COPY nuget-local/ nuget-local/
COPY AstraSystemsRental.Mail.Api/src/ AstraSystemsRental.Mail.Api/src/

RUN dotnet restore AstraSystemsRental.Mail.Api/src/AstraSystemsRental.Mail.Api/AstraSystemsRental.Mail.Api.csproj \
    --source https://api.nuget.org/v3/index.json \
    --source /src/nuget-local
RUN dotnet publish AstraSystemsRental.Mail.Api/src/AstraSystemsRental.Mail.Api/AstraSystemsRental.Mail.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/apiMail/health || exit 1

ENTRYPOINT ["dotnet", "AstraSystemsRental.Mail.Api.dll"]
