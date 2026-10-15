# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend-api.csproj ./
RUN dotnet restore backend-api.csproj

COPY . ./
RUN dotnet publish backend-api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish ./

# Patient uploads are written here; mount a persistent disk at /app/App_Data to keep them.
RUN mkdir -p /app/App_Data && chown -R app:app /app/App_Data
USER app

EXPOSE 8080
ENTRYPOINT ["dotnet", "backend-api.dll"]
