FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
ARG WKHTMLTOX_URL="https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6.1-3/wkhtmltox_0.12.6.1-3.bookworm_amd64.deb"
WORKDIR /app
EXPOSE 8080

RUN set -eux; \
    apt-get update; \
    apt-get install -y --no-install-recommends \
        ca-certificates wget xz-utils fontconfig libxrender1 libxext6 \
        libfreetype6 libjpeg62-turbo libpng16-16 libx11-6 libxcb1 \
        libxcomposite1 libxrandr2 libxi6 libxtst6 libglib2.0-0 libatk1.0-0 \
        xfonts-base xfonts-75dpi apt-utils; \
    wget -O /tmp/wkhtmltox.deb "$WKHTMLTOX_URL"; \
    apt-get install -y /tmp/wkhtmltox.deb || (cat /var/log/apt/term.log || true) && true; \
    command -v wkhtmltopdf >/dev/null 2>&1 || (echo "ERROR: wkhtmltopdf not found after install" && false); \
    rm -f /tmp/wkhtmltox.deb; \
    apt-get clean; rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SingularSystem.csproj", "."]
RUN dotnet restore "./SingularSystem.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./SingularSystem.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SingularSystem.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY Templates/ ./Templates/
COPY Resources/ ./Resources/
COPY Fonts/ ./Fonts/
ENV LD_LIBRARY_PATH="/usr/local/lib"
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "SingularSystem.dll"]
