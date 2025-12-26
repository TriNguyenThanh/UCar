# Development image - runs without publishing
FROM mcr.microsoft.com/dotnet/sdk:9.0
WORKDIR /app

# Copy csproj and restore dependencies
COPY ["UCar.csproj", "./"]
RUN dotnet restore "UCar.csproj"

# Copy everything else
COPY . .

# Expose port
EXPOSE 8080

# Run the application
ENTRYPOINT ["dotnet", "run", "--urls", "http://0.0.0.0:8080"]
