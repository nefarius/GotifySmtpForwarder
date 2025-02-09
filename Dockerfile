#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["GotifySmtpForwarder/GotifySmtpForwarder.csproj", "GotifySmtpForwarder/"]
COPY ["nuget.config", "GotifySmtpForwarder/"]
RUN dotnet restore "GotifySmtpForwarder/GotifySmtpForwarder.csproj"
COPY . .
WORKDIR "/src/GotifySmtpForwarder"
RUN dotnet build "GotifySmtpForwarder.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GotifySmtpForwarder.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GotifySmtpForwarder.dll"]