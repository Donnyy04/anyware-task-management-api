FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY AnywareTaskManagement.sln ./
COPY src/AnywareTaskManagement.Domain/*.csproj src/AnywareTaskManagement.Domain/
COPY src/AnywareTaskManagement.Application/*.csproj src/AnywareTaskManagement.Application/
COPY src/AnywareTaskManagement.Infrastructure/*.csproj src/AnywareTaskManagement.Infrastructure/
COPY src/AnywareTaskManagement.API/*.csproj src/AnywareTaskManagement.API/
RUN dotnet restore src/AnywareTaskManagement.API/AnywareTaskManagement.API.csproj
COPY src/ src/
RUN dotnet publish src/AnywareTaskManagement.API/AnywareTaskManagement.API.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "AnywareTaskManagement.API.dll"]
