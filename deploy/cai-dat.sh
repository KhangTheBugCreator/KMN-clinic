#!/bin/bash
# Cài hoặc cập nhật KMN Clinic trên máy chủ Linux có Docker. Chạy từ thư mục gốc repo:  bash deploy/cai-dat.sh
# Lần đầu: tạo deploy/.env với mật khẩu SQL và khóa JWT ngẫu nhiên. Lần sau (sau git pull): build lại web, giữ nguyên dữ liệu.
set -euo pipefail
cd "$(dirname "$0")"
command -v docker >/dev/null || { echo "Chưa có Docker. Cài bằng: curl -fsSL https://get.docker.com | sh"; exit 1; }
if [ ! -f .env ]; then
  ngau() { tr -dc 'A-Za-z0-9' < /dev/urandom | head -c "$1"; }
  cat > .env <<EOT
# Bí mật của máy chủ này, KHÔNG đưa lên Git. Mất file này thì web không vào được database.
SA_PASSWORD=Kmn$(ngau 20)!a1
JWT_KEY=$(ngau 64)
CONG_WEB=80
# 1 = lần đầu nạp dữ liệu mẫu (NV0001 quản trị viên...), 0 = database trống
NAP_DU_LIEU_MAU=1
EOT
  chmod 600 .env
  echo "Đã tạo deploy/.env"
fi
docker compose up -d --build
echo "Đang chờ website khởi động..."
cong=$(grep '^CONG_WEB=' .env | cut -d= -f2); cong=${cong:-80}
for i in $(seq 1 60); do
  if curl -fsS "http://localhost:$cong/api/he-thong/trang-thai" >/dev/null 2>&1; then
    ip=$(hostname -I | awk '{print $1}')
    echo "Website đang chạy: http://$ip:$cong (máy chủ thuê ngoài: thay $ip bằng IP công khai của máy chủ)"; exit 0
  fi
  sleep 3
done
echo "Website chưa trả lời. Xem log: docker compose -f deploy/docker-compose.yml logs --tail 50"; exit 1
