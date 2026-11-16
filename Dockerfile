FROM mcr.microsoft.com/dotnet/sdk:11.0-alpine AS sdk
RUN apk add --no-cache npm
WORKDIR /app
COPY . .
RUN --mount=type=secret,id=SIXLABORS_LICENSE_KEY,env=SIXLABORS_LICENSE_KEY \
    dotnet publish src/BabyTracker.csproj --runtime linux-musl-x64 --self-contained -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime-deps:11.0-alpine
RUN apk add --no-cache tzdata
WORKDIR /app
COPY --from=sdk /app/out .
ENTRYPOINT ["./BabyTracker"]