# Bước 1: Build dự án
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy các file solution và project để restore thư viện trước (tối ưu tốc độ build)
COPY *.sln .
COPY API/*.csproj ./API/
COPY BLL/*.csproj ./BLL/
COPY DAL/*.csproj ./DAL/
RUN dotnet restore

# Copy toàn bộ code và tiến hành Publish
COPY . .
WORKDIR /app/API
RUN dotnet publish -c Release -o /out

# Bước 2: Chạy ứng dụng
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out ./

# Cấu hình cổng 80 cho Render
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80

# Chỉ định file chạy chính
ENTRYPOINT ["dotnet", "API.dll"]