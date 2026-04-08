FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY . .

COPY BrainBusterV2.sln .
COPY /*.csproj ./BrainBuster/
COPY /Models ./Models/
COPY /Services ./Services/
COPY /brainbuster.db ./brainbuster.db

COPY /wwwroot ./wwwroot

RUN dotnet restore ./BrainBusterV2.csproj
RUN dotnet build ./BrainBusterV2.csproj -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/build .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "BrainBusterV2.dll"]