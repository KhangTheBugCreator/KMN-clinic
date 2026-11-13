#!/bin/bash
# Tạo database PHONGKHAM lần đầu. Đã có database thì không làm gì (không bao giờ xóa hay ghi đè dữ liệu).
set -euo pipefail
Q() { /opt/mssql-tools18/bin/sqlcmd -C -S db -U sa -P "$MSSQL_SA_PASSWORD" -b -I -f 65001 "$@"; }
co=$(Q -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name = 'PHONGKHAM'" | tr -d '[:space:]')
if [ "$co" = "1" ]; then echo "Database PHONGKHAM đã có, bỏ qua."; exit 0; fi
echo "Tạo cấu trúc database PHONGKHAM..."
Q -i /database/cau-truc-phongkham.sql > /dev/null
if [ "${NAP_DU_LIEU_MAU:-1}" = "1" ]; then
  echo "Nạp dữ liệu mẫu (nhân viên, khách hàng, khoa, phòng, ca, thuốc)..."
  Q -d PHONGKHAM -i /database/du-lieu-mau-phat-trien.sql > /dev/null
fi
echo "Xong."
