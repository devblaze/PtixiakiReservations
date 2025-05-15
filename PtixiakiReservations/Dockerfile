# Use the official ASP.NET Core runtime image as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the official .NET SDK image to build and publish the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file first for dependency restoration
COPY ["PtixiakiReservations/PtixiakiReservations.csproj", "PtixiakiReservations/"]
RUN dotnet restore "PtixiakiReservations/PtixiakiReservations.csproj"

# Copy the entire solution
COPY . .
WORKDIR "/src/PtixiakiReservations"
RUN dotnet build "PtixiakiReservations.csproj" -c Release -o /app/build

# Publish the application with important flags for static assets
FROM build AS publish
RUN dotnet publish "PtixiakiReservations.csproj" -c Release -o /app/publish \
    /p:UseAppHost=false \
    /p:SelfContained=false \
    /p:PublishTrimmed=false \
    /p:PreserveCompilationContext=true

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create the necessary NuGet directory structure with V5 subdirectory
RUN mkdir -p /home/app/.nuget/packages/microsoft.aspnetcore.identity.ui/8.0.12/staticwebassets/V5

# Set up correct permissions
RUN chown -R 1000:1000 /home/app

# Set the user for the container
USER 1000:1000

ENTRYPOINT ["dotnet", "PtixiakiReservations.dll"]