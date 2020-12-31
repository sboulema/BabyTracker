FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY *.sln .
COPY BabyTracker/*.csproj ./BabyTracker/
RUN dotnet restore

# copy everything else and build app
COPY BabyTracker/. ./BabyTracker/
WORKDIR /app/BabyTracker
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS runtime

RUN apt-get update -yq \
    && apt-get install curl gnupg -yq \
    && curl -sL https://deb.nodesource.com/setup_10.x | bash \
    && apt-get install nodejs -yq

WORKDIR /app
COPY --from=build /app/BabyTracker/out ./
VOLUME /data
ENTRYPOINT ["dotnet", "BabyTracker.dll"]