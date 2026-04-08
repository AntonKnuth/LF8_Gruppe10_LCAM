FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ./BrainBuster/BrainBusterV2.sln .
COPY ./BrainBuster/BrainBusterV2.csproj .
RUN dotnet restore ./BrainBuster/BrainBusterV2.csproj

COPY . .
RUN dotnet build ./BrainBuster/BrainBusterV2.csproj -c Release --no-restore
RUN dotnet publish ./BrainBuster/BrainBusterV2.csproj -c Release -o /app --no-build

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080

ENTRYPOINT ["dotnet", "BrainBusterV2.dll"]