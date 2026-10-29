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
ENTRYPOINT ["dotnet", "VitalTrek.API.dll"]
