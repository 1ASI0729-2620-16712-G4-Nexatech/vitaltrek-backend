FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /app
COPY VitalTrek.API/*.csproj VitalTrek.API/
RUN dotnet restore ./VitalTrek.API
COPY . .
RUN dotnet publish ./VitalTrek.API -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=builder /app/out .
EXPOSE 8080
# Cloud providers such as Render inject the listening port through the PORT variable.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-8080} exec dotnet VitalTrek.API.dll"]
