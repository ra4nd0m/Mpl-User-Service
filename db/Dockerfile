FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /app

COPY ["MplDbApi.sln", "./"]
COPY ["src/MplDbApi/MplDbApi.csproj", "src/MplDbApi/"]

RUN dotnet restore "src/MplDbApi/MplDbApi.csproj"

COPY . .
WORKDIR "/app/src/MplDbApi"
RUN dotnet publish "MplDbApi.csproj" -c Release -o /out --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
COPY --from=build /out .

EXPOSE 8080

ENTRYPOINT ["dotnet", "MplDbApi.dll"]