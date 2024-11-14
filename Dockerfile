FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS sdk
RUN apk add --no-cache npm
WORKDIR /app
COPY . .
RUN dotnet publish --runtime linux-musl-x64 --self-contained -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime-deps:9.0-alpine
RUN apk add --no-cache tzdata
WORKDIR /app
COPY --from=sdk /app/out .
ENTRYPOINT ["./BabyTracker"]