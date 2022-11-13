FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY *.sln .
COPY BabyTracker/*.csproj ./BabyTracker/
RUN dotnet restore

# copy everything else and build app
COPY BabyTracker/. ./BabyTracker/
WORKDIR /app/BabyTracker
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime

WORKDIR /app

COPY --from=build /app/BabyTracker/out ./

VOLUME /data

ENTRYPOINT ["dotnet", "BabyTracker.dll"]