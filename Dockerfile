# Estágio de Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["fabricawebrender.csproj", "./"]
RUN dotnet restore "fabricawebrender.csproj"
COPY . .
RUN dotnet publish "fabricawebrender.csproj" -c Release -o /app/publish

# Estágio Final / Execução
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "fabricawebrender.dll"]