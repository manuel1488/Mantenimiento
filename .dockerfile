# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0.310 AS build
WORKDIR /src

# Set build configuration
ARG BUILD_CONFIGURATION=Release

# Copy solution and project files
COPY ["App.sln", "."]
COPY ["src/App.Web/App.Web.csproj", "src/App.Web/"]
COPY ["src/App.Core/App.Core.csproj", "src/App.Core/"]
COPY ["src/App.Models/App.Models.csproj", "src/App.Models/"]
COPY ["src/App.Models.Data/App.Models.Data.csproj", "src/App.Models.Data/"]
COPY ["src/App.Services/App.Services.csproj", "src/App.Services/"]
COPY ["src/App.Shared/App.Shared.csproj", "src/App.Shared/"]
COPY ["tests/App.Services.Tests/App.Services.Tests.csproj", "tests/App.Services.Tests/"]

# Restore dependencies (solution-level to include test project)
RUN dotnet restore "App.sln"

# Copy everything else
COPY . .

# Build app
WORKDIR "/src/src/App.Web"
RUN dotnet build "App.Web.csproj" -c ${BUILD_CONFIGURATION} -o /app/build

# Stage 2: Test — fails the build if any test fails
FROM build AS test
WORKDIR /src
RUN dotnet test "tests/App.Services.Tests/App.Services.Tests.csproj" \
    -c ${BUILD_CONFIGURATION} \
    --no-restore \
    --logger "console;verbosity=normal"

# Stage 3: Publish (depends on test — if tests fail, publish never runs)
FROM test AS publish
WORKDIR "/src/src/App.Web"
RUN dotnet publish "App.Web.csproj" -c ${BUILD_CONFIGURATION}  -o /app/publish /p:UseAppHost=false

# Stage 4: Final
FROM mcr.microsoft.com/dotnet/aspnet:9.0.12 AS final
WORKDIR /app

# Install dependencies for i18n
RUN apt-get update \
    && apt-get install -y curl tzdata locales \
    && rm -rf /var/lib/apt/lists/* \
    # Generate all locales
    && sed -i 's/# \(en_US.UTF-8\)/\1/' /etc/locale.gen \
    && sed -i 's/# \(es_MX.UTF-8\)/\1/' /etc/locale.gen \
    && sed -i 's/# \(es_ES.UTF-8\)/\1/' /etc/locale.gen \
    && sed -i 's/# \(en_CA.UTF-8\)/\1/' /etc/locale.gen \
    && sed -i 's/# \(fr_CA.UTF-8\)/\1/' /etc/locale.gen \
    && locale-gen

# Install Chrome dependencies
RUN apt-get update \
    && apt-get install -y \
        libasound2 \
        libatk1.0-0 \
        libatk-bridge2.0-0 \
        libcairo2 \
        libcups2 \
        libdbus-1-3 \
        libexpat1 \
        libfontconfig1 \
        libgbm1 \
        libgcc1 \
        libglib2.0-0 \
        libnspr4 \
        libnss3 \
        libpango-1.0-0 \
        libpangocairo-1.0-0 \
        libx11-6 \
        libx11-xcb1 \
        libxcb1 \
        libxcomposite1 \
        libxcursor1 \
        libxdamage1 \
        libxext6 \
        libxfixes3 \
        libxi6 \
        libxrandr2 \
        libxrender1 \
        libxss1 \
        libxtst6 \
        fonts-liberation \
        fonts-noto-core \
        fonts-noto-extra \
        libappindicator3-1 \
        libnss3 \
        lsb-release \
        xdg-utils \
        wget \
        gnupg \
    && rm -rf /var/lib/apt/lists/*

# Install Chrome
RUN wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | gpg --dearmor -o /usr/share/keyrings/google-chrome.gpg \
    && echo "deb [arch=amd64 signed-by=/usr/share/keyrings/google-chrome.gpg] http://dl.google.com/linux/chrome/deb/ stable main" | tee /etc/apt/sources.list.d/google-chrome.list > /dev/null \
    && apt-get update \
    && apt-get install -y google-chrome-stable \
    && rm -rf /var/lib/apt/lists/*

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV LANGUAGE=en_US.UTF-8
ENV LANG=en_US.UTF-8
ENV LC_ALL=en_US.UTF-8
ENV TZ=UTC

# Chrome environment variables
ENV PUPPETEER_SKIP_CHROMIUM_DOWNLOAD=true
ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/google-chrome-stable

# Create directory and set permissions (ANTES de crear el usuario)
# Create directories and set permissions
RUN mkdir -p /app/wwwroot/uploads /app/Temp \
    && useradd -m appuser \
    && chown -R appuser:appuser /app \
    && chmod -R 755 /app

# Cambiar al usuario no-root
USER appuser

# Copy published app
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=30s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

# Expose port
EXPOSE 8080

# Set entry point
ENTRYPOINT ["dotnet", "App.Web.dll"]