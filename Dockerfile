FROM mcr.microsoft.com/dotnet/sdk:7.0 AS sdk
WORKDIR /app
COPY . .
RUN dotnet publish --runtime linux-musl-x64 -o out -p:PublishTrimmed=true

FROM mcr.microsoft.com/dotnet/runtime-deps:7.0-alpine
WORKDIR /app
COPY --from=sdk /app/out .
ENTRYPOINT ["./BabyTracker"]