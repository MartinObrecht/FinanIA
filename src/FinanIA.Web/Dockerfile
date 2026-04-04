FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/FinanIA.Web/FinanIA.Web.csproj src/FinanIA.Web/
RUN dotnet restore src/FinanIA.Web/FinanIA.Web.csproj

COPY src/FinanIA.Web/ src/FinanIA.Web/
RUN dotnet publish src/FinanIA.Web/FinanIA.Web.csproj -c Release -o /app/publish --no-restore

FROM nginx:alpine AS runtime
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY src/FinanIA.Web/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
