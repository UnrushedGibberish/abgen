FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/api/api.csproj src/api/
RUN dotnet restore src/api/api.csproj

COPY src/api/ src/api/
RUN dotnet publish src/api/api.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AbGen.Api.dll"]
