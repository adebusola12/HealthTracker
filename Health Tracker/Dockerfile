FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Health Tracker/HealthTracker.csproj", "Health Tracker/"]
RUN dotnet restore "Health Tracker/HealthTracker.csproj"

COPY . .
WORKDIR "/src/Health Tracker"
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "HealthTracker.dll"]