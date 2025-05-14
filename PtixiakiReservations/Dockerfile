# Use the official ASP.NET Core runtime image as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the official .NET SDK image to build and publish the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PtixiakiReservations/PtixiakiReservations.csproj", "PtixiakiReservations/"]
RUN dotnet restore "PtixiakiReservations/PtixiakiReservations.csproj"
COPY . .
WORKDIR "/src/PtixiakiReservations"
RUN dotnet build "PtixiakiReservations.csproj" -c Release -o /app/build

# Publish the application to a stage-specific location
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "PtixiakiReservations.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Use the official ASP.NET Core runtime image to run the published application
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PtixiakiReservations.dll"]