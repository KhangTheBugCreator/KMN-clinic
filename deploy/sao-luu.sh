#!/bin/bash
# Sao lưu database PHONGKHAM ra file .bak trong thư mục deploy/sao-luu/. Chạy:  sudo bash deploy/sao-luu.sh
set -euo pipefail
cd "$(dirname "$0")"
ten="phongkham-$(date +%Y%m%d-%H%M).bak"
docker compose exec -T db bash -c "/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P \"\$MSSQL_SA_PASSWORD\" -b -Q \"BACKUP DATABASE PHONGKHAM TO DISK = N'/var/opt/mssql/$ten' WITH INIT\""
mkdir -p sao-luu
docker compose cp "db:/var/opt/mssql/$ten" "sao-luu/$ten"
docker compose exec -T db rm -f "/var/opt/mssql/$ten"
echo "Đã sao lưu: deploy/sao-luu/$ten (mở được bằng SQL Server 2025 trở lên)"
