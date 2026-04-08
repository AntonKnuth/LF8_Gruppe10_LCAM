FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY . .

COPY *.sln .
COPY BrainBuster/Models ./Models
COPY BrainBuster/Services./Services
COPY BrainBuster/*.csproj ./BrainBuster/
COPY ./BrainBuster/brainbuster.db ./brainbuster.db

COPY ./BrainBuster/wwwroot ./wwwroot

RUN dotnet restore ./BrainBuster/BrainBusterV2.csproj
RUN dotnet build ./BrainBuster/BrainBusterV2.csproj -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/build .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "BrainBusterV2.dll"]