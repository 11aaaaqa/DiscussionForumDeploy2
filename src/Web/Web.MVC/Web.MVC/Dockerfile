# См. статью по ссылке https://aka.ms/customizecontainer, чтобы узнать как настроить контейнер отладки и как Visual Studio использует этот Dockerfile для создания образов для ускорения отладки.

# Этот этап используется при запуске из VS в быстром режиме (по умолчанию для конфигурации отладки)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update; apt-get install -y fontconfig
RUN fc-cache -f -v
USER $APP_UID
WORKDIR /app
EXPOSE 8080


# Этот этап используется для сборки проекта службы
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Web/Web.MVC/Web.MVC/Web.MVC.csproj", "src/Web/Web.MVC/Web.MVC/"]
COPY ["src/Libs/GeneralClassesLib/GeneralClassesLib.csproj", "src/Libs/GeneralClassesLib/"]
RUN dotnet restore "./src/Web/Web.MVC/Web.MVC/Web.MVC.csproj"
COPY . .
WORKDIR "/src/src/Web/Web.MVC/Web.MVC"
RUN dotnet build "./Web.MVC.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этот этап используется для публикации проекта службы, который будет скопирован на последний этап
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Web.MVC.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этот этап используется в рабочей среде или при запуске из VS в обычном режиме (по умолчанию, когда конфигурация отладки не используется)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Web.MVC.dll"]