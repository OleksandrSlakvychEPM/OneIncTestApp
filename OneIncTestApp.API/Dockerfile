# Use the official .NET SDK image for building the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the project files
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the app files and build the app
COPY . ./
RUN dotnet publish -c Release -o out

# Use the official .NET runtime image for running the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Expose the port the app runs on
EXPOSE 5000

# Start the app
ENTRYPOINT ["dotnet", "OneIncTestApp.API.dll"]