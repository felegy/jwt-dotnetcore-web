#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["JwtApp.csproj", "./"]
RUN dotnet restore "JwtApp.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "JwtApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "JwtApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JwtApp.dll"]
