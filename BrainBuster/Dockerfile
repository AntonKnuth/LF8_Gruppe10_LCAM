FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY BrainBusterV2.sln .
COPY BrainBusterV2.csproj .
COPY BrainBusterV2.Tests/BrainBusterV2.Tests.csproj BrainBusterV2.Tests/
RUN dotnet restore

COPY . .
RUN dotnet build -c Release --no-restore
RUN dotnet publish -c Release -o /app --no-build

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080

ENTRYPOINT ["dotnet", "BrainBusterV2.dll"]