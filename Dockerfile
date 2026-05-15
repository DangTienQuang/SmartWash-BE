
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY *.sln .
COPY API/*.csproj ./API/
COPY BLL/*.csproj ./BLL/
COPY DAL/*.csproj ./DAL/
RUN dotnet restore

COPY . .
WORKDIR /app/API
RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out ./

ENV DOTNET_gcServer=0
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "API.dll"]