# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["UCar.csproj", "./"]
RUN dotnet restore "UCar.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "UCar.csproj" -c Release -o /app/build

# # Publish stage
# FROM build AS publish
# RUN dotnet publish "UCar.csproj" -c Release -o /app/publish /p:UseAppHost=false

# # Runtime stage
# FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
# WORKDIR /app
# EXPOSE 8080

# COPY --from=publish /app/publish .
# ENTRYPOINT ["dotnet", "UCar.dll"]

# Run
ENTRYPOINT [ "dotnet", "run" ]