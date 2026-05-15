# 1. Build
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

# 2. Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out ./

# --- LIỀU THUỐC ĐẶC TRỊ LỖI 139 TRÊN RENDER ---
# 1. Tắt Server Garbage Collection
ENV DOTNET_gcServer=0

# 2. TẮT Diagnostics (Thủ phạm chính gây xung đột và văng app)
ENV DOTNET_EnableDiagnostics=0
ENV COMPlus_EnableDiagnostics=0

# 3. Tắt đa ngôn ngữ (Tiết kiệm RAM tối đa, tránh lỗi thiếu thư viện Linux)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# 4. Port chuẩn cho Render
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
# ----------------------------------------------

ENTRYPOINT ["dotnet", "API.dll"]