# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution metadata first so restore is cached unless packages change.
COPY global.json TierLedger.slnx ./
COPY src/TierLedger.Domain/TierLedger.Domain.csproj src/TierLedger.Domain/
COPY src/TierLedger.Application/TierLedger.Application.csproj src/TierLedger.Application/
COPY src/TierLedger.Infrastructure/TierLedger.Infrastructure.csproj src/TierLedger.Infrastructure/
COPY src/TierLedger.Api/TierLedger.Api.csproj src/TierLedger.Api/
RUN dotnet restore src/TierLedger.Api/TierLedger.Api.csproj

COPY . .
RUN dotnet publish src/TierLedger.Api/TierLedger.Api.csproj -c Release -o /app/publish --no-restore

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# The aspnet image already ships a non-root "app" user.
USER app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "TierLedger.Api.dll"]
