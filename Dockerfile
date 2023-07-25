FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS sdk
RUN apk add --no-cache npm
WORKDIR /app
COPY . .
RUN dotnet publish --runtime linux-musl-x64 --self-contained -c Release -o out -p:PublishTrimmed=true

FROM mcr.microsoft.com/dotnet/runtime-deps:7.0-alpine
RUN apk add --no-cache tzdata
WORKDIR /app
COPY --from=sdk /app/out .
ENTRYPOINT ["./BabyTracker"]