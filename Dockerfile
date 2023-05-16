FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS sdk
WORKDIR /app
COPY . .
RUN dotnet publish --runtime linux-musl-x64 -c Release -o out -p:PublishTrimmed=true -p:PublishSingleFile=true

FROM mcr.microsoft.com/dotnet/runtime-deps:7.0-alpine
RUN apk add --no-cache tzdata
WORKDIR /app
COPY --from=sdk /app/out .
ENTRYPOINT ["./BabyTracker"]