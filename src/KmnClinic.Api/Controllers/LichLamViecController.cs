using KmnClinic.Api.Data;
using KmnClinic.Api.Infrastructure;
using KmnClinic.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KmnClinic.Api.Controllers;

/// <summary>
/// Lịch làm việc: quản trị viên xếp lịch; lễ tân, thu ngân xem toàn bộ; bác sĩ chỉ xem lịch của mình.
/// </summary>
[ApiController]
[Route("api/lich-lam-viec")]
[Authorize(Roles = VaiTro.NhanVien)]
public class LichLamViecController : ControllerBase
{
    private const int SoNgayToiDa = 62;
    private readonly LichLamViecRepository _repo;
    public LichLamViecController(LichLamViecRepository repo) => _repo = repo;

    private static DateOnly HomNay => DateOnly.FromDateTime(DateTime.Today);
    private bool LaBacSi => User.IsInRole(VaiTro.BacSi);

    /// <summary>Mặc định: tuần hiện tại (Thứ 2 đến Chủ nhật). Tối đa 62 ngày một lần.</summary>
    [HttpGet]
    public Task<List<LichLamViecItem>> DanhSach([FromQuery] LichLamViecQuery q, CancellationToken ct)
    {
        var tu = q.TuNgay ?? HomNay.AddDays(-(((int)HomNay.DayOfWeek + 6) % 7));
        var den = q.DenNgay ?? tu.AddDays(6);
        if (den < tu) throw AppException.BadRequest("Ngày kết thúc phải sau ngày bắt đầu.");
        if (den.DayNumber - tu.DayNumber + 1 > SoNgayToiDa) throw AppException.BadRequest($"Chỉ xem tối đa {SoNgayToiDa} ngày một lần.");
        if (LaBacSi) q.MaNv = User.MaNguoiDung(); // bác sĩ chỉ xem lịch của mình
        return _repo.DanhSachAsync(q, tu, den, ct);
    }

    [HttpGet("{ma}")]
    public async Task<LichLamViecItem> ChiTiet(string ma, CancellationToken ct)
    {
        var lich = await _repo.TimAsync(ma, ct);
        if (lich is null || (LaBacSi && lich.MaNv != User.MaNguoiDung())) throw AppException.NotFound("Không tìm thấy lịch làm việc.");
        return lich;
    }

    /// <summary>Danh sách bác sĩ để chọn khi xếp lịch, lọc lịch (không lộ số điện thoại).</summary>
    [HttpGet("bac-si")]
    public Task<List<BacSiItem>> BacSi([FromQuery] string? maKhoa, CancellationToken ct) => _repo.DanhSachBacSiAsync(maKhoa, ct);

    [HttpPost]
    [Authorize(Roles = VaiTro.QuanTriVien)]
    public async Task<LichLamViecItem> Them(LichLamViecRequest req, CancellationToken ct)
    {
        KiemTraNgay(req.NgayLamViec!.Value);
        return (await _repo.TimAsync(await _repo.ThemAsync(req, ct), ct))!;
    }

    [HttpPost("hang-loat")]
    [Authorize(Roles = VaiTro.QuanTriVien)]
    public async Task<XepLichHangLoatResponse> ThemHangLoat(XepLichHangLoatRequest req, CancellationToken ct)
    {
        DateOnly tu = req.TuNgay!.Value, den = req.DenNgay!.Value;
        KiemTraNgay(tu);
        if (den < tu) throw AppException.BadRequest("Ngày kết thúc phải sau ngày bắt đầu.");
        if (den.DayNumber - tu.DayNumber + 1 > SoNgayToiDa) throw AppException.BadRequest($"Chỉ xếp tối đa {SoNgayToiDa} ngày một lần.");
        if (req.CacThu.Any(t => t is < 0 or > 6)) throw AppException.BadRequest("Thứ trong tuần không hợp lệ.");
        var thu = req.CacThu.ToHashSet();
        var cacNgay = Enumerable.Range(0, den.DayNumber - tu.DayNumber + 1).Select(tu.AddDays)
            .Where(d => thu.Contains((int)d.DayOfWeek)).ToList();
        if (cacNgay.Count == 0) throw AppException.BadRequest("Không có ngày nào khớp các thứ đã chọn trong khoảng ngày này.");
        var ma = await _repo.ThemHangLoatAsync(cacNgay, new LichLamViecRequest { MaNv = req.MaNv, MaPhong = req.MaPhong, MaCa = req.MaCa }, ct);
        return new(ma.Count, ma);
    }

    [HttpPut("{ma}")]
    [Authorize(Roles = VaiTro.QuanTriVien)]
    public async Task<LichLamViecItem> Sua(string ma, LichLamViecRequest req, CancellationToken ct)
    {
        KiemTraNgay(req.NgayLamViec!.Value);
        if (!await _repo.SuaAsync(ma, req, ct)) throw AppException.NotFound("Không tìm thấy lịch làm việc.");
        return (await _repo.TimAsync(ma, ct))!;
    }

    [HttpDelete("{ma}")]
    [Authorize(Roles = VaiTro.QuanTriVien)]
    public async Task<IActionResult> Xoa(string ma, CancellationToken ct)
    {
        if (!await _repo.XoaAsync(ma, ct)) throw AppException.NotFound("Không tìm thấy lịch làm việc.");
        return Ok(new { message = "Đã xóa lịch làm việc." });
    }

    /// <summary>
    /// Chủ phòng khám quyết định (12/11/2026): chỉ xếp lịch từ ngày mai trở đi, chặt hơn RB9 (không xếp ngày đã qua).
    /// Trigger RB9 chỉ chặn khi thêm; backend áp dụng cả khi sửa.
    /// </summary>
    private static void KiemTraNgay(DateOnly ngay)
    {
        if (ngay < HomNay) throw AppException.BadRequest("Không được xếp lịch làm việc cho ngày trong quá khứ (RB9).");
        if (ngay == HomNay) throw AppException.BadRequest("Chỉ xếp lịch làm việc từ ngày mai trở đi.");
        if (ngay > HomNay.AddYears(1)) throw AppException.BadRequest("Chỉ xếp lịch trong vòng một năm tới.");
    }
}
