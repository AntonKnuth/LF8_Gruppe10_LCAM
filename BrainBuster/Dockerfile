FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy only project files first (better caching)
COPY BrainBuster/*.csproj ./BrainBuster/
RUN dotnet restore ./BrainBuster/BrainBusterV2.csproj

# Copy the rest of the source
COPY BrainBuster/. ./BrainBuster/

WORKDIR /app/BrainBuster
RUN dotnet build ./BrainBuster/BrainBusterV2.csproj -c Release -o /app/build

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/build .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "BrainBusterV2.dll"]