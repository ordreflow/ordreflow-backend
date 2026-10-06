# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0.406 AS build
WORKDIR /src

# Copy project metadata first so dependency restore can be cached separately
# from the application source.
COPY global.json ./
COPY .config/dotnet-tools.json .config/dotnet-tools.json
COPY Backend/WebApi/WebApi.csproj Backend/WebApi/
COPY Backend/Application/Application.csproj Backend/Application/
COPY Backend/ObjectMapper/ObjectMapper.csproj Backend/ObjectMapper/
COPY Backend/Persistence/Persistence.csproj Backend/Persistence/
COPY Backend/Domain/Domain.csproj Backend/Domain/
COPY Backend/OperationResultPattern/OperationResultPattern.csproj Backend/OperationResultPattern/

RUN dotnet restore Backend/WebApi/WebApi.csproj

COPY Backend/ Backend/

# This target is useful for a separate Compose/Kubernetes migration job.
# It keeps the SDK and source code out of the runtime API image.
FROM build AS migration
RUN dotnet tool restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish Backend/WebApi/WebApi.csproj \
    --configuration $BUILD_CONFIGURATION \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0.13 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=publish /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "WebApi.dll"]
