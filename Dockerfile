# Ảnh Docker của website KMN Clinic (API + giao diện trong wwwroot). Dùng cùng deploy/docker-compose.yml.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/KmnClinic.Api/KmnClinic.Api.csproj src/KmnClinic.Api/
RUN dotnet restore src/KmnClinic.Api/KmnClinic.Api.csproj
COPY src/ src/
RUN dotnet publish src/KmnClinic.Api/KmnClinic.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
# Giờ Việt Nam: nghiệp vụ (ca khám, đặt lịch, tiếp nhận) tính theo giờ máy chủ web
ENV TZ=Asia/Ho_Chi_Minh \
    ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "KmnClinic.Api.dll"]
