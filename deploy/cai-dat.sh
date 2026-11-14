#!/bin/bash
# Cài hoặc cập nhật KMN Clinic trên máy chủ Linux có Docker. Chạy từ thư mục gốc repo:  bash deploy/cai-dat.sh
# Có tên miền (đã trỏ bản ghi A về IP máy chủ) thì bật https:  bash deploy/cai-dat.sh kmnclinic.click
# Lần đầu: tạo deploy/.env với mật khẩu SQL và khóa JWT ngẫu nhiên. Lần sau (sau git pull): build lại web, giữ nguyên dữ liệu.
set -euo pipefail
cd "$(dirname "$0")"
command -v docker >/dev/null || { echo "Chưa có Docker. Cài bằng: curl -fsSL https://get.docker.com | sh"; exit 1; }
# VPS nhỏ (2GB RAM): tạo 2GB swap một lần để máy không bị treo khi RAM tăng đột ngột
if [ "$(swapon --show --noheadings | wc -l)" = "0" ] && [ ! -f /swapfile ]; then
  echo "Tạo 2GB swap..."
  fallocate -l 2G /swapfile && chmod 600 /swapfile && mkswap /swapfile >/dev/null && swapon /swapfile
  grep -q '^/swapfile' /etc/fstab || echo '/swapfile none swap sw 0 0' >> /etc/fstab
fi
if [ ! -f .env ]; then
  ngau() { tr -dc 'A-Za-z0-9' < /dev/urandom | head -c "$1"; }
  cat > .env <<EOT
# Bí mật của máy chủ này, KHÔNG đưa lên Git. Mất file này thì web không vào được database.
SA_PASSWORD=Kmn$(ngau 20)!a1
JWT_KEY=$(ngau 64)

# 1 = lần đầu nạp dữ liệu mẫu (NV0001 quản trị viên...), 0 = database trống
NAP_DU_LIEU_MAU=1
EOT
  chmod 600 .env
  echo "Đã tạo deploy/.env"
fi
if [ -n "${1:-}" ]; then
  ten=${1#http*://}; ten=${ten%%/*}; ten=${ten#www.}
  sed -i '/^TEN_MIEN=/d' .env
  echo "TEN_MIEN=\"$ten, www.$ten\"" >> .env
  echo "Đã đặt tên miền: $ten (https tự bật nếu bản ghi A đã trỏ về máy chủ này)"
fi
docker compose up -d --build --remove-orphans
echo "Đang chờ website khởi động..."
for i in $(seq 1 60); do
  if curl -fsS "http://localhost:8080/api/he-thong/trang-thai" >/dev/null 2>&1; then
    ten=$(grep '^TEN_MIEN=' .env | cut -d'"' -f2 | cut -d, -f1)
    if [ -n "$ten" ]; then echo "Website đang chạy: https://$ten (lần đầu chờ 1-2 phút để có chứng chỉ https)"
    else ip=$(hostname -I | awk '{print $1}'); echo "Website đang chạy: http://$ip (máy chủ thuê ngoài: thay $ip bằng IP công khai của máy chủ)"; fi
    exit 0
  fi
  sleep 3
done
echo "Website chưa trả lời. Xem log: docker compose -f deploy/docker-compose.yml logs --tail 50"; exit 1
