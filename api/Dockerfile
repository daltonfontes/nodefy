# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src
COPY . .
RUN dotnet restore Nodefy.Api/Nodefy.Api.csproj || true
RUN dotnet publish Nodefy.Api/Nodefy.Api.csproj -c Release -o /out --no-restore || true

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "Nodefy.Api.dll"]
