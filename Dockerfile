FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY ["CapstoneProject_BE/capstone_be.csproj", "CapstoneProject_BE/"]
COPY ["BusinessObjects/BusinessObjects.csproj", "BusinessObjects/"]
COPY ["Services/Services.csproj", "Services/"]
COPY ["DataAccess/DataAccess.csproj", "DataAccess/"]
COPY ["Repositories/Repositories.csproj", "Repositories/"]

RUN dotnet restore CapstoneProject_BE/capstone_be.csproj

COPY . .

WORKDIR /src/CapstoneProject_BE
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
	CMD curl -f http://localhost:8080/health || exit 1
EXPOSE 8080
ENTRYPOINT ["dotnet", "capstone_be.dll"]