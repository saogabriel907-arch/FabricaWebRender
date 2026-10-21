# Estágio de Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["fabricawebrender.csproj", "./"]
RUN dotnet restore "fabricawebrender.csproj"
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Estágio de Execução
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://*:$PORT
EXPOSE 8080

ENTRYPOINT ["dotnet", "FabricaWebRender.dll"]