# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj
COPY PtixiakiReservations/PtixiakiReservations.csproj PtixiakiReservations/

# Restore
RUN dotnet restore PtixiakiReservations/PtixiakiReservations.csproj

# Copy everything
COPY . .

# Publish
RUN dotnet publish PtixiakiReservations/PtixiakiReservations.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PtixiakiReservations.dll"]
