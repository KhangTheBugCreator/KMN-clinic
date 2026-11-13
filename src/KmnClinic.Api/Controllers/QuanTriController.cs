using KmnClinic.Api.Data;
using KmnClinic.Api.Infrastructure;
using KmnClinic.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KmnClinic.Api.Controllers;

/// <summary>
/// Quản trị viên giám sát vận hành (M10): xem lịch hẹn, hồ sơ khám, hóa đơn của toàn phòng khám.
/// CHỈ ĐỌC: việc xác nhận, khám, thu tiền vẫn do đúng vai trò thực hiện để giữ truy vết người làm (RB17, RB32, RB34).
/// Dùng lại truy vấn của lễ tân, bác sĩ, thu ngân, không nhân bản SQL.
/// </summary>
[ApiController]
[Route("api/quan-tri")]
[Authorize(Roles = VaiTro.QuanTriVien)]
public class QuanTriController : ControllerBase
{
    private readonly LeTanRepository _leTan;
    private readonly BacSiRepository _bacSi;
    private readonly ThuNganRepository _thuNgan;

    public QuanTriController(LeTanRepository leTan, BacSiRepository bacSi, ThuNganRepository thuNgan)
        => (_leTan, _bacSi, _thuNgan) = (leTan, bacSi, thuNgan);

    [HttpGet("lich-hen")]
    public Task<TrangKetQua<LichHenLeTanItem>> LichHen([FromQuery] LichHenLeTanQuery tk, CancellationToken ct)
    {
        if (tk.TrangThai is { Length: > 0 } tt && !TrangThaiLichHen.TatCa.Contains(tt))
            throw AppException.BadRequest("Trạng thái lịch hẹn không hợp lệ.");
        return _leTan.DanhSachLichHenAsync(tk, ct);
    }

    [HttpGet("lich-hen/{ma}")]
    public async Task<LichHenLeTanItem> ChiTietLichHen(string ma, CancellationToken ct)
        => await _leTan.TimLichHenAsync(ma, ct) ?? throw AppException.NotFound("Không tìm thấy lịch hẹn.");

    [HttpGet("ho-so")]
    public Task<TrangKetQua<HoSoBacSiItem>> HoSo([FromQuery] HoSoQuanTriQuery tk, CancellationToken ct)
    {
        if (tk.TrangThai is { Length: > 0 } tt && !TrangThaiHoSo.TatCa.Contains(tt))
            throw AppException.BadRequest("Trạng thái hồ sơ không hợp lệ.");
        if (tk.TuNgay is { } tu && tk.DenNgay is { } den && tu > den)
            throw AppException.BadRequest("Từ ngày phải trước hoặc bằng đến ngày.");
        // MaNv trống = mọi bác sĩ
        return _bacSi.LichSuAsync(string.IsNullOrWhiteSpace(tk.MaNv) ? null : tk.MaNv.Trim(), tk, ct);
    }

    [HttpGet("ho-so/{ma}")]
    public async Task<HoSoChiTiet> ChiTietHoSo(string ma, CancellationToken ct)
        => await _bacSi.ChiTietAsync(ma, null, ct) ?? throw AppException.NotFound("Không tìm thấy hồ sơ khám.");

    [HttpGet("hoa-don")]
    public Task<TrangKetQua<HoaDonItem>> HoaDon([FromQuery] HoaDonQuery tk, CancellationToken ct)
    {
        if (tk.TrangThai is { Length: > 0 } tt && !TrangThaiHoaDon.TatCa.Contains(tt))
            throw AppException.BadRequest("Trạng thái hóa đơn không hợp lệ.");
        if (tk.TuNgay is { } tu && tk.DenNgay is { } den && tu > den)
            throw AppException.BadRequest("Từ ngày phải trước hoặc bằng đến ngày.");
        tk.CuaToi = false; // quản trị viên không thu tiền
        return _thuNgan.DanhSachAsync(tk, "", ct);
    }

    [HttpGet("hoa-don/{ma}")]
    public async Task<HoaDonChiTiet> ChiTietHoaDon(string ma, CancellationToken ct)
        => await _thuNgan.ChiTietAsync(ma, ct) ?? throw AppException.NotFound("Không tìm thấy hóa đơn.");
}
