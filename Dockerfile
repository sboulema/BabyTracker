FROM mcr.microsoft.com/dotnet/sdk:7.0 AS sdk
WORKDIR /app
COPY . .
RUN dotnet publish -o out

FROM mcr.microsoft.com/dotnet/runtime-deps:7.0-alpine
WORKDIR /app
COPY --from=sdk /app/out .
ENTRYPOINT ["dotnet", "BabyTracker.dll"]