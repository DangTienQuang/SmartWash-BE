# 1. Giai đoạn Build (Dùng Alpine để nhẹ và nhanh)
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /app

# Copy các file project để restore trước
COPY *.sln .
COPY API/*.csproj ./API/
COPY BLL/*.csproj ./BLL/
COPY DAL/*.csproj ./DAL/
RUN dotnet restore

# Copy toàn bộ code và build
COPY . .
WORKDIR /app/API
# Publish ra một thư mục riêng
RUN dotnet publish -c Release -o /out /p:UseAppHost=false

# 2. Giai đoạn Runtime (Dùng Alpine - Siêu nhẹ cho RAM 512MB)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
COPY --from=build /out ./

# --- CẤU HÌNH ĐẶC TRỊ RENDER FREE (BẮT BUỘC) ---
# Tắt chế độ server GC để không ngốn RAM
ENV DOTNET_gcServer=0
# Tắt hoàn toàn chẩn đoán (Tránh lỗi 139)
ENV DOTNET_EnableDiagnostics=0
# Tắt đa ngôn ngữ để app nhẹ nhất có thể
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# Port cho Render
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
# -----------------------------------------------

# LƯU Ý: Nếu file project của bạn không phải tên "API.csproj" 
# thì hãy đổi tên "API.dll" ở dưới thành tên project của bạn nhé.
ENTRYPOINT ["dotnet", "API.dll"]