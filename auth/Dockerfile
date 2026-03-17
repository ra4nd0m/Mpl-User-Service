FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /app

COPY ["MplAuthService.sln", "./"]
COPY ["src/MplAuthService/MplAuthService.csproj", "src/MplAuthService/"]

RUN dotnet restore "src/MplAuthService/MplAuthService.csproj"

COPY . .
WORKDIR "/app/src/MplAuthService"
RUN dotnet publish "MplAuthService.csproj" -c Release -o /out --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
COPY --from=build /out .

EXPOSE 8080

ENTRYPOINT ["dotnet", "MplAuthService.dll"]