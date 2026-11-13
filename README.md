# KMN Clinic – Website quản lý phòng khám

Website gồm phần công khai (giới thiệu, chuyên khoa, bác sĩ, đặt lịch) và hệ thống nội bộ cho
quản trị viên, lễ tân, bác sĩ, thu ngân. Chạy trên database **PHONGKHAM** có sẵn, không thay đổi cấu trúc database.

| Thư mục | Nội dung |
|---|---|
| `src/KmnClinic.Api` | ASP.NET Core 8 Web API (ADO.NET, JWT) và giao diện trong `wwwroot` |
| `tests/KmnClinic.Tests` | Kiểm thử tự động (xUnit), chạy trên database kiểm thử |
| `database/du-lieu-mau-phat-trien.sql` | Dữ liệu mẫu để chạy thử, **không dùng cho phòng khám thật** |
| `design/` | Bộ giao diện tĩnh đã duyệt ở Giai đoạn 2 (bản tham chiếu) |

## Yêu cầu

- .NET SDK 8 (máy chỉ có .NET 10 vẫn chạy được, xem bước 4)
- SQL Server 2019 trở lên đã khôi phục database `PHONGKHAM` (từ file backup hoặc `PHONGKHAM-SCRIPT.sql`)
- Trình duyệt Chrome, Edge hoặc Firefox bản mới. Mọi thư viện giao diện (Bootstrap, Chart.js, jsPDF, ExcelJS, phông chữ)
  đã nằm sẵn trong `wwwroot/assets/vendor`, chạy được khi không có mạng.

## Chạy lần đầu

1. Lấy mã nguồn: `git clone https://github.com/KhangTheBugCreator/KMN-clinic.git` (repo riêng tư, cần đăng nhập GitHub).
2. Nạp dữ liệu mẫu (tùy chọn, để có sẵn nhân viên, khách hàng, khoa, phòng, ca, thuốc):
   mở `database/du-lieu-mau-phat-trien.sql` trong SQL Server Management Studio và chạy trên database `PHONGKHAM`.
3. Cấu hình bí mật bằng User Secrets (không ghi vào `appsettings.json`, không đưa lên Git):

   ```bash
   cd src/KmnClinic.Api
   dotnet user-secrets set "ConnectionStrings:PhongKham" "Server=localhost\\SQLEXPRESS;Database=PHONGKHAM;Trusted_Connection=True;TrustServerCertificate=True"
   dotnet user-secrets set "Jwt:Key" "<chuỗi ngẫu nhiên tối thiểu 32 ký tự>"
   ```

   Có thể dùng biến môi trường `ConnectionStrings__PhongKham` và `Jwt__Key` thay cho User Secrets.
4. Chạy trong thư mục `src/KmnClinic.Api`: `dotnet run` rồi mở `http://localhost:5080/`.
   Máy chỉ có .NET 10 thì chạy `dotnet run -p:RollForward=Major`.
5. Kiểm tra nhanh: `http://localhost:5080/api/he-thong/trang-thai` trả `"database":"ok"` là đã nối được database.

Website **không có đăng nhập** (quyết định 12/11/2026): mở `http://localhost:5080/chon-vai-tro.html`, chọn vai trò
rồi chọn một người có sẵn trong database (hoặc tạo khách hàng mới). Mọi API vẫn phân quyền theo vai trò đã chọn.
Với dữ liệu mẫu: quản trị viên NV0001, lễ tân NV0002, bác sĩ NV0003 và NV0004, thu ngân NV0005, khách hàng KH0001.

> Vì không có đăng nhập, ai mở được website cũng chọn được mọi vai trò. Chỉ dùng trên máy hoặc mạng nội bộ để học tập,
> trình diễn. Trước khi dùng cho phòng khám thật cần làm đăng nhập thật thay cho trang Chọn vai trò.

## Đi thử một lượt khám đầy đủ

1. **Quản trị viên** (NV0001) → Lịch làm việc: xếp ca cho bác sĩ từ ngày mai trở đi.
   Muốn thử trong hôm nay, thêm ca bằng SQL hoặc đổi ngày giờ Windows sau khi đã xếp lịch.
2. **Khách hàng** → Đặt lịch khám: chọn khoa, bác sĩ, ca còn chỗ. Xem lại ở Lịch hẹn của tôi.
3. **Lễ tân** (NV0002) → Lịch hẹn: xác nhận; đúng ngày khám thì bấm Tiếp nhận để tạo hồ sơ khám.
   Khách không hẹn: Tiếp nhận khách vãng lai, chọn bác sĩ đang trực.
4. **Bác sĩ** → Hàng chờ khám: ghi chẩn đoán, lời dặn, tiền công, kê thuốc, bấm Hoàn tất để lập hóa đơn.
5. **Thu ngân** (NV0005) → Hóa đơn chờ thu: xác nhận đã thu (kho thuốc trừ lúc này), in hoặc xuất PDF A4.
6. **Quản trị viên** → Dashboard, Báo cáo Excel; mục Vận hành xem lại lịch hẹn, hồ sơ khám, hóa đơn (chỉ xem).

## Đưa lên máy chủ chạy 24/7 (Linux + Docker)

Cần một máy chủ Linux (Ubuntu 22.04/24.04) **tối thiểu 2 GB RAM** (SQL Server cần chừng đó), mở cổng 80.
Web và SQL Server 2025 Express chạy trong Docker, tự bật lại khi máy khởi động lại hoặc khi bị lỗi.

1. Đăng nhập máy chủ bằng SSH, cài Docker và Git:

   ```bash
   curl -fsSL https://get.docker.com | sudo sh
   sudo apt-get install -y git
   ```
2. Lấy mã nguồn (repo riêng tư: GitHub hỏi mật khẩu thì dán Personal Access Token) rồi cài:

   ```bash
   git clone https://github.com/KhangTheBugCreator/KMN-clinic.git
   cd KMN-clinic
   sudo bash deploy/cai-dat.sh
   ```

   Lần đầu script tạo `deploy/.env` chứa mật khẩu SQL và khóa JWT ngẫu nhiên (không đưa lên Git, đừng xóa),
   tạo database `PHONGKHAM` từ `database/cau-truc-phongkham.sql` và nạp dữ liệu mẫu. Muốn database trống thì sửa
   `NAP_DU_LIEU_MAU=0` trong `deploy/.env` trước lần chạy đầu.
3. Mở `http://<IP máy chủ>/chon-vai-tro.html`.

**Cập nhật khi có code mới:** `cd KMN-clinic && git pull && sudo bash deploy/cai-dat.sh`. Dữ liệu được giữ nguyên
(nằm trong volume Docker `kmn-clinic_dulieu-sql`); database đã có thì không bị tạo lại.

**Lệnh hay dùng** (chạy trong thư mục `deploy`):

| Việc | Lệnh |
|---|---|
| Xem trạng thái | `sudo docker compose ps` |
| Xem log web | `sudo docker compose logs web --tail 100` |
| Khởi động lại | `sudo docker compose restart web` |
| Sao lưu database ra file `.bak` | `sudo bash deploy/sao-luu.sh` (chạy ở thư mục gốc repo) |
| Tắt hẳn (giữ dữ liệu) | `sudo docker compose down` |

> Website không có đăng nhập: ai biết địa chỉ cũng chọn được mọi vai trò. Chỉ dùng cho học tập, chia sẻ địa chỉ
> trong nhóm; không nhập dữ liệu thật của bệnh nhân.

## Kiểm thử tự động

```bash
export KMN_TEST_DB="Server=...;Database=PHONGKHAM;...;TrustServerCertificate=True"
dotnet test
```

Kiểm thử cần database có dữ liệu mẫu và nên dùng một bản database riêng để thử. Dữ liệu tạm do kiểm thử tạo ra
được xóa ở cuối mỗi test; nếu tiến trình kiểm thử bị dừng giữa chừng, có thể còn sót dòng có mã bắt đầu bằng `LT`, `CT`
hoặc khách hàng tên "KT ...", xóa tay các dòng này trước khi chạy lại.

Bộ kiểm thử gồm: phân quyền cho từng API theo ma trận đã chốt (`PhanQuyenTests`), một lượt khám đi hết các vai trò
(`ToanLuongTests`), và kiểm thử riêng từng module (danh mục, nhân viên, lịch, đặt lịch, lễ tân, bác sĩ, thu ngân, thống kê).

## Xử lý sự cố thường gặp

| Hiện tượng | Cách xử lý |
|---|---|
| `api/he-thong/trang-thai` báo lỗi hoặc trang báo "Không kết nối được máy chủ" | Kiểm tra SQL Server đang chạy và chuỗi kết nối trong User Secrets |
| Báo thiếu `Jwt:Key` khi chạy | Đặt `Jwt:Key` (ít nhất 32 ký tự) như bước 3 |
| Báo không tìm thấy .NET 8 (Microsoft.AspNetCore.App 8.0) khi chạy | Cài .NET 8 hoặc chạy `dotnet run -p:RollForward=Major` |
| Ảnh nhân viên không hiện | Đường dẫn phải là HTTPS trỏ thẳng tới file ảnh (ví dụ `https://i.ibb.co/...`); ảnh lỗi hoặc trống sẽ hiện ảnh mặc định |
| Không đặt lịch được | Ca phải chưa bắt đầu và bác sĩ còn lịch; lịch chỉ xếp từ ngày mai trở đi |

## Quy ước chính

- Mọi lỗi API trả về JSON `{ "message": "..." }` bằng tiếng Việt. Lỗi từ trigger (THROW 500xx) được trả nguyên câu.
- Mã khóa chính tự sinh dạng tiền tố + số: `KH0001`, `NV0001`, `LH0001`, `HS0001`, `HD0001`...
- Không đăng nhập: vai trò được giả lập ở trang Chọn vai trò; website không đọc cột mật khẩu.
- Phân quyền kiểm tra ở backend cho từng API; việc ẩn nút ở giao diện chỉ để tiện dùng.

## Tiến độ theo module

- [x] **M0 Nền tảng**: kết nối SQL, xử lý lỗi, sinh mã, giả lập vai trò, phân quyền theo vai trò, bộ giao diện chung
- [x] **M1 Danh mục**: khoa, phòng làm việc (kèm lịch sử dùng phòng), ca làm việc, thuốc (lọc sắp hết; bác sĩ và thu ngân chỉ xem)
- [x] **M2 Nhân viên, khách hàng**: quản trị viên quản lý nhân viên (ảnh qua đường dẫn HTTPS, mật khẩu chỉ nhập khi thêm và được băm); quản trị viên và lễ tân quản lý khách hàng
- [x] **M3 Lịch làm việc**: lịch tuần, tháng, ngày; quản trị viên xếp từng lịch hoặc nhiều ngày một lần, sửa, xóa; lễ tân xem toàn bộ, bác sĩ xem lịch của mình
- [x] **M4 Website công khai**: trang chủ, chuyên khoa, bác sĩ và lịch khám còn đặt được
- [x] **M5 Đặt lịch**: khách đặt, xem, tự hủy lịch hẹn; chống đặt trùng
- [x] **M6 Lễ tân**: xác nhận, hủy lịch hẹn; tiếp nhận khách có hẹn và khách vãng lai
- [x] **M7 Bác sĩ**: hàng chờ, khám, kê đơn, hoàn tất lập hóa đơn; xem lịch sử
- [x] **M8 Thu ngân**: thu tiền, trừ kho, in và xuất hóa đơn PDF A4
- [x] **M9 Thống kê**: dashboard quản trị theo kỳ, 5 báo cáo Excel
- [x] **M10 Hoàn thiện**: trang vận hành chỉ xem cho quản trị viên, lịch hẹn của bác sĩ, kiểm thử phân quyền từng API và toàn luồng, rà giao diện điện thoại, hướng dẫn chạy
