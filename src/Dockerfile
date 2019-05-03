#FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
#WORKDIR /app
#EXPOSE 80
#
#FROM microsoft/dotnet:2.2-sdk AS build
#WORKDIR /src
#COPY ["src/Foundation.ObjectService.WebUI.csproj", "src/"]
#RUN dotnet restore "src/Foundation.ObjectService.WebUI.csproj"
#COPY . .
#WORKDIR "/src/src"
#RUN dotnet build "Foundation.ObjectService.WebUI.csproj" -c Release -o /app
#
#FROM build AS publish
#RUN dotnet publish "Foundation.ObjectService.WebUI.csproj" -c Release -o /app
#
#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app .
#ENTRYPOINT ["dotnet", "Foundation.ObjectService.WebUI.dll"]



FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore "Foundation.ObjectService.WebUI.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "Foundation.ObjectService.WebUI.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Foundation.ObjectService.WebUI.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
# don't run as root user
RUN chown 1001:0 /app/Foundation.ObjectService.WebUI.dll
RUN chmod g+rwx /app/Foundation.ObjectService.WebUI.dll
USER 1001
ENTRYPOINT ["dotnet", "Foundation.ObjectService.WebUI.dll"]






#FROM microsoft/dotnet:2.2-sdk AS build-env
#WORKDIR /app
#
## Copy csproj and restore as distinct layers
#COPY *.csproj ./
#RUN dotnet restore
#
## Copy everything else and build
#COPY . ./
#RUN dotnet publish -c Release -o out
#
## Build runtime image
#FROM microsoft/dotnet:aspnetcore-runtime
#WORKDIR /app
#COPY --from=build-env /app/out .
#ENTRYPOINT ["dotnet", "Foundation.ObjectService.WebUI.dll"]