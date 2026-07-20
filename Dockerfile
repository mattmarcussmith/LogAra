FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY LogAra.slnx ./
COPY src ./src

RUN dotnet restore ./LogAra.slnx
RUN dotnet publish ./src/LogAra.Client/LogAra.Client.csproj \
    -c Release \
    -o /out/client \
    --no-restore

RUN dotnet publish ./src/LogAra.Api/LogAra.Api.csproj \
    -c Release \
    -o /out/api \
    --no-restore

# Serve the Blazor WASM application from the API container.
RUN rm -rf /out/api/wwwroot \
    && mkdir -p /out/api/wwwroot \
    && cp -a /out/client/wwwroot/. /out/api/wwwroot/

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /out/api ./

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=10000

EXPOSE 10000

ENTRYPOINT ["dotnet", "LogAra.Api.dll"]
