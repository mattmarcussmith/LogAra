FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY LogAra.slnx ./
COPY src ./src

RUN dotnet restore ./LogAra.slnx
RUN dotnet publish ./src/LogAra.Client/LogAra.Client.csproj -c Release -o /out/client
RUN dotnet publish ./src/LogAra.Api/LogAra.Api.csproj -c Release -o /out/api

# Publish a single production container that serves both API and WASM assets.
RUN rm -rf /out/api/wwwroot && mkdir -p /out/api/wwwroot
RUN cp -a /out/client/wwwroot/. /out/api/wwwroot/

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /out/api ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "LogAra.Api.dll"]
