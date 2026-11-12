using KmnClinic.Api.Data;
using KmnClinic.Api.Infrastructure;
using KmnClinic.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KmnClinic.Api.Controllers;

/// <summary>Quản lý nhân viên: chỉ quản trị viên. Không trả về và không cho sửa mật khẩu.</summary>
[ApiController]
[Route("api/nhan-vien")]
[Authorize(Roles = VaiTro.QuanTriVien)]
public class NhanVienController : ControllerBase
{
    private readonly NhanSuRepository _repo;
    public NhanVienController(NhanSuRepository repo) => _repo = repo;

    [HttpGet]
    public Task<TrangKetQua<NhanVienItem>> DanhSach([FromQuery] NhanVienQuery tk, CancellationToken ct) => _repo.DanhSachNhanVienAsync(tk, ct);

    [HttpGet("{ma}")]
    public async Task<NhanVienItem> ChiTiet(string ma, CancellationToken ct)
        => await _repo.TimNhanVienAsync(ma, ct) ?? throw AppException.NotFound("Không tìm thấy nhân viên.");

    [HttpPost]
    public async Task<NhanVienItem> Them(NhanVienRequest req, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(req.MatKhau)) throw AppException.BadRequest("Vui lòng nhập mật khẩu (tối thiểu 8 ký tự).");
        var anh = await KiemTra(req, ct);
        var ma = await _repo.ThemNhanVienAsync(req, anh, MatKhau.Bam(req.MatKhau), ct);
        return (await _repo.TimNhanVienAsync(ma, ct))!;
    }

    [HttpPut("{ma}")]
    public async Task<NhanVienItem> Sua(string ma, NhanVienRequest req, CancellationToken ct)
    {
        var anh = await KiemTra(req, ct);
        if (!await _repo.SuaNhanVienAsync(ma, req, anh, User.MaNguoiDung(), ct)) throw AppException.NotFound("Không tìm thấy nhân viên.");
        return (await _repo.TimNhanVienAsync(ma, ct))!;
    }

    [HttpDelete("{ma}")]
    public async Task<IActionResult> Xoa(string ma, CancellationToken ct)
    {
        if (!await _repo.XoaNhanVienAsync(ma, User.MaNguoiDung(), ct)) throw AppException.NotFound("Không tìm thấy nhân viên.");
        return Ok(new { message = "Đã xóa nhân viên." });
    }

    /// <summary>RB6: chỉ bác sĩ có khoa. Ngày sinh hợp lệ. Ảnh chỉ nhận URL HTTPS công khai. Trả về URL ảnh đã chuẩn hóa.</summary>
    private async Task<string?> KiemTra(NhanVienRequest req, CancellationToken ct)
    {
        NgaySinh.KiemTra(req.NgaySinh);
        if (req.ChucVu == "Bác sĩ")
        {
            if (string.IsNullOrWhiteSpace(req.MaKhoa)) throw AppException.BadRequest("Bác sĩ phải thuộc một khoa (RB6).");
            req.MaKhoa = req.MaKhoa.Trim();
            if (!await _repo.KhoaTonTaiAsync(req.MaKhoa, ct)) throw AppException.BadRequest("Khoa đã chọn không tồn tại.");
        }
        else req.MaKhoa = null; // RB6: nhân viên khác không thuộc khoa nào
        return AnhUrl.ChuanHoa(req.DuongDanAnh);
    }
}

/// <summary>Khách hàng: quản trị viên và lễ tân xem, thêm, sửa; chỉ quản trị viên xóa.</summary>
[ApiController]
[Route("api/khach-hang")]
[Authorize(Roles = VaiTro.QuanTriVien + "," + VaiTro.LeTan)]
public class KhachHangController : ControllerBase
{
    private readonly NhanSuRepository _repo;
    public KhachHangController(NhanSuRepository repo) => _repo = repo;

    [HttpGet]
    public Task<TrangKetQua<KhachHangItem>> DanhSach([FromQuery] KhachHangQuery tk, CancellationToken ct) => _repo.DanhSachKhachHangAsync(tk, ct);

    [HttpGet("{ma}")]
    public async Task<KhachHangItem> ChiTiet(string ma, CancellationToken ct)
        => await _repo.TimKhachHangAsync(ma, ct) ?? throw AppException.NotFound("Không tìm thấy khách hàng.");

    [HttpPost]
    public async Task<KhachHangItem> Them(KhachHangRequest req, CancellationToken ct)
    {
        NgaySinh.KiemTra(req.NgaySinh);
        return (await _repo.TimKhachHangAsync(await _repo.ThemKhachHangAsync(req, ct), ct))!;
    }

    [HttpPut("{ma}")]
    public async Task<KhachHangItem> Sua(string ma, KhachHangRequest req, CancellationToken ct)
    {
        NgaySinh.KiemTra(req.NgaySinh);
        if (!await _repo.SuaKhachHangAsync(ma, req, ct)) throw AppException.NotFound("Không tìm thấy khách hàng.");
        return (await _repo.TimKhachHangAsync(ma, ct))!;
    }

    [HttpDelete("{ma}")]
    [Authorize(Roles = VaiTro.QuanTriVien)]
    public async Task<IActionResult> Xoa(string ma, CancellationToken ct)
    {
        if (!await _repo.XoaKhachHangAsync(ma, ct)) throw AppException.NotFound("Không tìm thấy khách hàng.");
        return Ok(new { message = "Đã xóa khách hàng." });
    }
}

internal static class NgaySinh
{
    public static void KiemTra(DateOnly? ngay)
    {
        if (ngay is null) return;
        var homNay = DateOnly.FromDateTime(DateTime.Today);
        if (ngay > homNay) throw AppException.BadRequest("Ngày sinh không được ở tương lai.");
        if (ngay < homNay.AddYears(-130)) throw AppException.BadRequest("Ngày sinh không hợp lệ.");
    }
}
