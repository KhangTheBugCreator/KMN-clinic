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

- .NET SDK 8
- SQL Server đã khôi phục database `PHONGKHAM` (từ file backup hoặc `PHONGKHAM-SCRIPT.sql`)

## Chạy lần đầu

1. Nạp dữ liệu mẫu (tùy chọn, để có tài khoản đăng nhập thử):
   mở `database/du-lieu-mau-phat-trien.sql` trong SQL Server Management Studio và chạy.
2. Cấu hình bí mật bằng User Secrets (không ghi vào `appsettings.json`):

   ```bash
   cd src/KmnClinic.Api
   dotnet user-secrets set "ConnectionStrings:PhongKham" "Server=localhost\\SQLEXPRESS;Database=PHONGKHAM;Trusted_Connection=True;TrustServerCertificate=True"
   dotnet user-secrets set "Jwt:Key" "<chuỗi ngẫu nhiên tối thiểu 32 ký tự>"
   ```

   Có thể dùng biến môi trường `ConnectionStrings__PhongKham` và `Jwt__Key` thay cho User Secrets.
3. Chạy: `dotnet run` rồi mở `http://localhost:5080/`. Máy chỉ có .NET 10 thì chạy `dotnet run -p:RollForward=Major`.

Website **không có đăng nhập** (quyết định 12/11/2026): mở `http://localhost:5080/chon-vai-tro.html`, chọn vai trò
rồi chọn một người có sẵn trong database (hoặc tạo khách hàng mới). Mọi API vẫn phân quyền theo vai trò đã chọn.
Với dữ liệu mẫu: quản trị viên NV0001, lễ tân NV0002, bác sĩ NV0003 và NV0004, thu ngân NV0005, khách hàng KH0001.

## Kiểm thử tự động

```bash
export KMN_TEST_DB="Server=...;Database=PHONGKHAM;...;TrustServerCertificate=True"
dotnet test
```

Kiểm thử cần database có dữ liệu mẫu; mọi thay đổi trong lúc kiểm thử đều được hoàn tác.

## Quy ước chính

- Mọi lỗi API trả về JSON `{ "message": "..." }` bằng tiếng Việt. Lỗi từ trigger (THROW 500xx) được trả nguyên câu.
- Mã khóa chính tự sinh dạng tiền tố + số: `KH0001`, `NV0001`, `LH0001`, `HS0001`, `HD0001`...
- Không đăng nhập: vai trò được giả lập ở trang Chọn vai trò; website không đọc cột mật khẩu.
- Phân quyền kiểm tra ở backend cho từng API; việc ẩn nút ở giao diện chỉ để tiện dùng.

## Tiến độ theo module

- [x] **M0 Nền tảng**: kết nối SQL, xử lý lỗi, sinh mã, giả lập vai trò, phân quyền theo vai trò, bộ giao diện chung
- [ ] M1 Danh mục · M2 Nhân viên, khách hàng · M3 Lịch làm việc · M4 Website công khai · M5 Khách hàng đặt lịch
- [ ] M6 Lễ tân · M7 Bác sĩ · M8 Thu ngân · M9 Thống kê, báo cáo · M10 Hoàn thiện

Các trang còn nhãn "Nội dung mẫu · chưa nối dữ liệu" sẽ được nối API ở module tương ứng.
