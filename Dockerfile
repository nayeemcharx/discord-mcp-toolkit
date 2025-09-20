##### Use .NET 9.0 SDK on Alpine
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build

# Set the working directory
WORKDIR /app

COPY ./*.csproj ./

# Restore using the solution
RUN dotnet restore 

# Copy application files and plugins
COPY . /app


ENTRYPOINT ["dotnet","run"]