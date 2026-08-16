# syntax=docker/dockerfile:1

# --- Build stage: restore + publish on the .NET 8 SDK image -----------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Restore first so dependency downloads are cached across code-only changes.
COPY Zamfara.Web/Zamfara.Web.csproj ./Zamfara.Web/
RUN dotnet restore Zamfara.Web/Zamfara.Web.csproj
COPY Zamfara.Web/ ./Zamfara.Web/
RUN dotnet publish Zamfara.Web/Zamfara.Web.csproj -c Release -o /app/publish --no-restore

# --- Runtime stage: minimal ASP.NET Core 8 image, non-root, HTTP only ------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
# Production env activates HSTS, HTTPS redirection and the error page. The
# HTTPS port lets the redirection middleware build correct https:// URLs even
# though the container itself listens on plain HTTP.
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_HTTPS_PORT=443 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1
EXPOSE 8080
# SQLite lives in a volume so the DB survives container recreation and stays
# writable even when the container runs with a read-only root filesystem.
# Create the directory as root first, then hand it to the app user.
RUN mkdir -p /app/App_Data && chown $APP_UID:$APP_UID /app/App_Data
VOLUME /app/App_Data
# Run as the non-root 'app' user (uid 1654) provided by the base image.
USER $APP_UID
COPY --chown=$APP_UID:$APP_UID --from=build /app/publish .
# TCP-connect + HTTP/1.0 probe (no curl/wget in the runtime image).
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD bash -c 'exec 3<>/dev/tcp/127.0.0.1/8080; printf "GET /healthz HTTP/1.0\r\nHost: localhost\r\n\r\n" >&3; head -n 1 <&3 | grep -q "200"'
ENTRYPOINT ["dotnet", "Zamfara.Web.dll"]
