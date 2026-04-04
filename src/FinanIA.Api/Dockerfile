FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for layer caching
COPY src/FinanIA.Domain/FinanIA.Domain.csproj src/FinanIA.Domain/
COPY src/FinanIA.Application/FinanIA.Application.csproj src/FinanIA.Application/
COPY src/FinanIA.Infrastructure/FinanIA.Infrastructure.csproj src/FinanIA.Infrastructure/
COPY src/FinanIA.Api/FinanIA.Api.csproj src/FinanIA.Api/
RUN dotnet restore src/FinanIA.Api/FinanIA.Api.csproj

# Copy source and publish
COPY src/FinanIA.Domain/ src/FinanIA.Domain/
COPY src/FinanIA.Application/ src/FinanIA.Application/
COPY src/FinanIA.Infrastructure/ src/FinanIA.Infrastructure/
COPY src/FinanIA.Api/ src/FinanIA.Api/
RUN dotnet publish src/FinanIA.Api/FinanIA.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "FinanIA.Api.dll"]
