FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /app

COPY ["MplUserApi.sln", "./"]
COPY ["src/MplUserService/MplUserService.csproj", "src/MplUserService/"]

RUN dotnet restore "src/MplUserService/MplUserService.csproj"

COPY . .
WORKDIR "/app/src/MplUserService"
RUN dotnet publish "MplUserService.csproj" -c Release -o /out --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
COPY --from=build /out .

EXPOSE 8080

ENTRYPOINT ["dotnet", "MplUserService.dll"]