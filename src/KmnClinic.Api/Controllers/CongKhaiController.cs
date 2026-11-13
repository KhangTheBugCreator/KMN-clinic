using KmnClinic.Api.Data;
using KmnClinic.Api.Infrastructure;
using KmnClinic.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KmnClinic.Api.Controllers;

/// <summary>
/// API cho website công khai (M4): không cần đăng nhập, chỉ đọc. Chỉ trả tên, khoa, ảnh, ca trực, tên phòng;
/// không trả số điện thoại, ngày sinh hay số lượng lịch hẹn. Chỉ trả các ca còn đặt được (RB16).
/// </summary>
[ApiController]
[Route("api/cong-khai")]
[AllowAnonymous]
public class CongKhaiController : ControllerBase
{
    private const int SoNgayToiDa = 14;
    private readonly CongKhaiRepository _repo;
    public CongKhaiController(CongKhaiRepository repo) => _repo = repo;

    private static DateTime BayGio => DateTime.Now;
    private static DateOnly HomNay => DateOnly.FromDateTime(BayGio);

    [HttpGet("tong-quan")]
    public Task<TongQuanCongKhai> TongQuan(CancellationToken ct) => _repo.TongQuanAsync(BayGio, ct);

    [HttpGet("khoa")]
    public Task<List<KhoaCongKhai>> Khoa(CancellationToken ct) => _repo.DanhSachKhoaAsync(ct);

    /// <summary>Bác sĩ (có phân trang), mỗi người kèm tối đa 3 ca còn đặt được trong 7 ngày tới, hoặc trong đúng ngày lọc.</summary>
    [HttpGet("bac-si")]
    public Task<TrangKetQua<BacSiCongKhai>> BacSi([FromQuery] BacSiCongKhaiQuery tk, CancellationToken ct)
    {
        if (tk.Ngay is { } n && (n < HomNay || n > HomNay.AddDays(SoNgayToiDa - 1)))
            throw AppException.BadRequest($"Chỉ xem lịch khám trong {SoNgayToiDa} ngày tới.");
        return _repo.DanhSachBacSiAsync(tk, Math.Clamp(tk.PageSize, 1, 24), 7, 3, BayGio, ct);
    }

    /// <summary>Thông tin một bác sĩ và lịch còn đặt được trong 14 ngày tới.</summary>
    [HttpGet("bac-si/{ma}")]
    public async Task<BacSiCongKhai> ChiTietBacSi(string ma, CancellationToken ct)
        => await _repo.TimBacSiAsync(ma, SoNgayToiDa, BayGio, ct) ?? throw AppException.NotFound("Không tìm thấy bác sĩ.");

    /// <summary>Các ca còn đặt được từ <c>tuNgay</c> (mặc định hôm nay), tối đa 14 ngày, trong 14 ngày tới.</summary>
    [HttpGet("lich-kham")]
    public Task<List<LichKhamCongKhai>> LichKham([FromQuery] LichKhamQuery tk, CancellationToken ct)
    {
        var tu = tk.TuNgay ?? HomNay;
        if (tu < HomNay) tu = HomNay;
        if (tk.SoNgay is < 1 or > SoNgayToiDa) throw AppException.BadRequest($"Số ngày phải từ 1 đến {SoNgayToiDa}.");
        var den = tu.AddDays(tk.SoNgay - 1);
        var gioiHan = HomNay.AddDays(SoNgayToiDa - 1);
        if (tu > gioiHan) return Task.FromResult(new List<LichKhamCongKhai>());
        if (den > gioiHan) den = gioiHan;
        return _repo.LichKhamAsync(tu, den, tk.MaKhoa, tk.MaNv, BayGio, ct);
    }
}
