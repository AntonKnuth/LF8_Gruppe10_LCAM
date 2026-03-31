FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY . .

COPY *.sln .
COPY BrainBuster/*.csproj ./BrainBuster/
COPY ./BrainBuster/brainbuster.db ./brainbuster.db

RUN dotnet restore ./BrainBuster/BrainBuster.csproj
RUN dotnet build ./BrainBuster/BrainBuster.csproj -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/build .
COPY --from=build /app/wwwroot ./wwwroot

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "BrainBuster.dll"]