using KmnClinic.Api.Data;
using KmnClinic.Api.Infrastructure;
using KmnClinic.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KmnClinic.Api.Controllers;

/// <summary>Khoa: quản trị viên ghi, mọi nhân viên đọc.</summary>
[ApiController]
[Route("api/khoa")]
[Authorize(Roles = VaiTro.NhanVien)]
public class KhoaController : ControllerBase
{
    private readonly DanhMucRepository _repo;
    public KhoaController(DanhMucRepository repo) => _repo = repo;

    [HttpGet]
    public Task<TrangKetQua<KhoaItem>> DanhSach([FromQuery] TimKiemQuery tk, CancellationToken ct) => _repo.DanhSachKhoaAsync(tk, ct);

    [HttpGet("{ma}")]
    public async Task<KhoaItem> ChiTiet(string ma, CancellationToken ct)
        => await _repo.TimKhoaAsync(ma, ct) ?? throw AppException.NotFound("Không tìm thấy khoa.");

    [HttpPost]
    [Authorize(Roles = VaiTro.QuanTriVien)]
    public async Task<KhoaItem> Them(KhoaRequest req, CancellationToken ct)
        => (await _repo.TimKhoaAsync(await _repo.ThemKhoaAsync(req, ct), ct))!;

    [HttpPut("{ma}")]
    [Authorize(Roles = VaiTro.QuanTriVien)]
    public async Task<KhoaItem> Sua(string ma, KhoaRequest req, CancellationToken ct)
    {
        if (!await _repo.SuaKhoaAsync(ma, req, ct)) throw AppException.NotFound("Không tìm thấy khoa.");
        return (await _repo.TimKhoaAsync(ma, ct))!;
    }

    [HttpDelete("{ma}")]
    [Authorize(Roles = VaiTro.QuanTriVien)]
    public async Task<IActionResult> Xoa(string ma, CancellationToken ct)
    {
        if (!await _repo.XoaKhoaAsync(ma, ct)) throw AppException.NotFound("Không tìm thấy khoa.");
        return Ok(new { message = "Đã xóa khoa." });
    }
}

/// <summary>Phòng làm việc: quản trị viên ghi, mọi nhân viên đọc (lễ tân, lịch làm việc cần tên phòng).</summary>
[ApiController]
[Route("api/phong")]
[Authorize(Roles = VaiTro.NhanVien)]
public class PhongController : ControllerBase
{
    private readonly DanhMucRepository _repo;
    public PhongController(DanhMucRepository repo) => _repo = repo;

    [HttpGet]
    public Task<TrangKetQua<PhongItem>> DanhSach([FromQuery] TimKiemQuery tk, [FromQuery] string? maKhoa, CancellationToken ct)
        => _repo.DanhSachPhongAsync(tk, maKhoa, ct);

    [HttpGet("{ma}")]
    public async Task<PhongItem> ChiTiet(string ma, CancellationToken ct)
        => await _repo.TimPhongAsync(ma, ct) ?? throw AppException.NotFound("Không tìm thấy phòng.");

    [HttpGet("{ma}/lich-su-dung")]
    [Authorize(Roles = VaiTro.QuanTriVien)]
    public async Task<TrangKetQua<LichSuDungPhongItem>> LichSuDung(string ma, [FromQuery] TimKiemQuery tk, CancellationToken ct)
    {
        _ = await _repo.TimPhongAsync(ma, ct) ?? throw AppException.NotFound("Không tìm thấy phòng.");
        return await _repo.LichSuDungPhongAsync(ma, tk, ct);
    }

    [HttpPost]
    [Authorize(Roles = VaiTro.QuanTriVien)]
    public async Task<PhongItem> Them(PhongRequest req, CancellationToken ct)
        => (await _repo.TimPhongAsync(await _repo.ThemPhongAsync(req, ct), ct))!;

    [HttpPut("{ma}")]
    [Authorize(Roles = VaiTro.QuanTriVien)]
    public async Task<PhongItem> Sua(string ma, PhongRequest req, CancellationToken ct)
    {
        if (!await _repo.SuaPhongAsync(ma, req, ct)) throw AppException.NotFound("Không tìm thấy phòng.");
        return (await _repo.TimPhongAsync(ma, ct))!;
    }

    [HttpDelete("{ma}")]
    [Authorize(Roles = VaiTro.QuanTriVien)]
    public async Task<IActionResult> Xoa(string ma, CancellationToken ct)
    {
        if (!await _repo.XoaPhongAsync(ma, ct)) throw AppException.NotFound("Không tìm thấy phòng.");
        return Ok(new { message = "Đã xóa phòng." });
    }
}

/// <summary>Ca làm việc: quản trị viên ghi, mọi nhân viên đọc.</summary>
[ApiController]
[Route("api/ca")]
[Authorize(Roles = VaiTro.NhanVien)]
public class CaController : ControllerBase
{
    private readonly DanhMucRepository _repo;
    public CaController(DanhMucRepository repo) => _repo = repo;

    [HttpGet]
    public Task<List<CaItem>> DanhSach(CancellationToken ct) => _repo.DanhSachCaAsync(ct);

    [HttpGet("{ma}")]
    public async Task<CaItem> ChiTiet(string ma, CancellationToken ct)
        => await _repo.TimCaAsync(ma, ct) ?? throw AppException.NotFound("Không tìm thấy ca.");

    [HttpPost]
    [Authorize(Roles = VaiTro.QuanTriVien)]
    public async Task<CaItem> Them(CaRequest req, CancellationToken ct)
    {
        KiemTraGio(req);
        return (await _repo.TimCaAsync(await _repo.ThemCaAsync(req, ct), ct))!;
    }

    [HttpPut("{ma}")]
    [Authorize(Roles = VaiTro.QuanTriVien)]
    public async Task<CaItem> Sua(string ma, CaRequest req, CancellationToken ct)
    {
        KiemTraGio(req);
        if (!await _repo.SuaCaAsync(ma, req, ct)) throw AppException.NotFound("Không tìm thấy ca.");
        return (await _repo.TimCaAsync(ma, ct))!;
    }

    [HttpDelete("{ma}")]
    [Authorize(Roles = VaiTro.QuanTriVien)]
    public async Task<IActionResult> Xoa(string ma, CancellationToken ct)
    {
        if (!await _repo.XoaCaAsync(ma, ct)) throw AppException.NotFound("Không tìm thấy ca.");
        return Ok(new { message = "Đã xóa ca." });
    }

    /// <summary>RB3 (database cũng chặn bằng CK_Ca_lam_viec_gio); kiểm tra trước để báo lỗi sớm.</summary>
    private static void KiemTraGio(CaRequest req)
    {
        if (req.GioBatDau >= req.GioKetThuc) throw AppException.BadRequest("Giờ bắt đầu ca phải nhỏ hơn giờ kết thúc.");
    }
}

/// <summary>Thuốc: quản trị viên ghi; bác sĩ và thu ngân đọc. Không có xóa để giữ lịch sử đơn thuốc.</summary>
[ApiController]
[Route("api/thuoc")]
[Authorize(Roles = VaiTro.QuanTriVien + "," + VaiTro.BacSi + "," + VaiTro.ThuNgan)]
public class ThuocController : ControllerBase
{
    private readonly DanhMucRepository _repo;
    public ThuocController(DanhMucRepository repo) => _repo = repo;

    [HttpGet]
    public Task<TrangKetQua<ThuocItem>> DanhSach([FromQuery] TimKiemQuery tk, [FromQuery] bool sapHet, CancellationToken ct)
        => _repo.DanhSachThuocAsync(tk, sapHet, ct);

    /// <summary>Thuốc có tồn ≤ ngưỡng (mặc định 20, chỉnh ở appsettings PhongKham:NguongThuocSapHet).</summary>
    [HttpGet("sap-het")]
    public Task<TrangKetQua<ThuocItem>> SapHet([FromQuery] TimKiemQuery tk, CancellationToken ct)
        => _repo.DanhSachThuocAsync(tk, true, ct);

    [HttpGet("{ma}")]
    public async Task<ThuocItem> ChiTiet(string ma, CancellationToken ct)
        => await _repo.TimThuocAsync(ma, ct) ?? throw AppException.NotFound("Không tìm thấy thuốc.");

    [HttpPost]
    [Authorize(Roles = VaiTro.QuanTriVien)]
    public async Task<ThuocItem> Them(ThuocRequest req, CancellationToken ct)
        => (await _repo.TimThuocAsync(await _repo.ThemThuocAsync(req, ct), ct))!;

    [HttpPut("{ma}")]
    [Authorize(Roles = VaiTro.QuanTriVien)]
    public async Task<ThuocItem> Sua(string ma, ThuocRequest req, CancellationToken ct)
    {
        if (!await _repo.SuaThuocAsync(ma, req, ct)) throw AppException.NotFound("Không tìm thấy thuốc.");
        return (await _repo.TimThuocAsync(ma, ct))!;
    }
}
