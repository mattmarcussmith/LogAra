FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY LogAra.slnx ./
COPY src ./src

RUN dotnet restore ./LogAra.slnx

RUN dotnet publish ./src/LogAra.Client/LogAra.Client.csproj \
    -c Release \
    -o /out/client \
    --no-restore

RUN test -d /out/client/wwwroot/_framework \
    && find /out/client/wwwroot/_framework -maxdepth 1 -type f \
    -name "icudt*.dat" -print

RUN dotnet publish ./src/LogAra.Api/LogAra.Api.csproj \
    -c Release \
    -o /out/api \
    --no-restore

RUN rm -rf /out/api/wwwroot \
    && mkdir -p /out/api/wwwroot \
    && cp -a /out/client/wwwroot/. /out/api/wwwroot/ \
    && test -n "$(find /out/api/wwwroot/_framework -maxdepth 1 -name 'icudt*.dat' -print -quit)"

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /out/api ./

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "LogAra.Api.dll"]
