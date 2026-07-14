# ── Build ──────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY TicketClassifier.Api/*.csproj TicketClassifier.Api/
RUN dotnet restore TicketClassifier.Api/TicketClassifier.Api.csproj
COPY . .
RUN dotnet publish TicketClassifier.Api/TicketClassifier.Api.csproj -c Release -o /app

# ── Runtime ────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TicketClassifier.Api.dll"]
