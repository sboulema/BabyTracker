FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY *.sln .
COPY BabyTracker/*.csproj ./BabyTracker/
RUN dotnet restore

# copy everything else and build app
COPY BabyTracker/. ./BabyTracker/
WORKDIR /app/BabyTracker
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime
WORKDIR /app
COPY --from=build /app/BabyTracker/out ./
ENV DATA_DIRECTORY data
ENTRYPOINT ["dotnet", "BabyTracker.dll"]