FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Microservices/Bookmark/BookmarkMicroservice.Api/BookmarkMicroservice.Api.csproj", "src/Microservices/Bookmark/BookmarkMicroservice.Api/"]
RUN dotnet restore "./src/Microservices/Bookmark/BookmarkMicroservice.Api/./BookmarkMicroservice.Api.csproj"
COPY . .
WORKDIR "/src/src/Microservices/Bookmark/BookmarkMicroservice.Api"
RUN dotnet build "./BookmarkMicroservice.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BookmarkMicroservice.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookmarkMicroservice.Api.dll"]