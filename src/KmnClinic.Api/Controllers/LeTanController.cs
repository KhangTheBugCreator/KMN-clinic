using KmnClinic.Api.Data;
using KmnClinic.Api.Infrastructure;
using KmnClinic.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KmnClinic.Api.Controllers;

/// <summary>
/// Lễ tân (M6): xem, xác nhận, hủy lịch hẹn; tiếp nhận khách có hẹn và khách vãng lai thành hồ sơ khám.
/// Mã lễ tân thực hiện luôn lấy từ token (RB17, RB34 truy vết đúng người).
/// </summary>
[ApiController]
[Route("api/le-tan")]
[Authorize(Roles = VaiTro.LeTan)]
public class LeTanController : ControllerBase
{
    private readonly LeTanRepository _repo;
    public LeTanController(LeTanRepository repo) => _repo = repo;

    private static DateTime BayGio => DateTime.Now;
    private string MaLeTan => User.MaNguoiDung();

    [HttpGet("tong-quan")]
    public Task<TongQuanLeTan> TongQuan(CancellationToken ct) => _repo.TongQuanAsync(BayGio, ct);

    [HttpGet("lich-hen")]
    public Task<TrangKetQua<LichHenLeTanItem>> DanhSachLichHen([FromQuery] LichHenLeTanQuery tk, CancellationToken ct)
    {
        if (tk.TrangThai is { Length: > 0 } tt && !TrangThaiLichHen.TatCa.Contains(tt))
            throw AppException.BadRequest("Trạng thái lịch hẹn không hợp lệ.");
        return _repo.DanhSachLichHenAsync(tk, ct);
    }

    [HttpGet("lich-hen/{ma}")]
    public async Task<LichHenLeTanItem> ChiTietLichHen(string ma, CancellationToken ct)
        => await _repo.TimLichHenAsync(ma, ct) ?? throw AppException.NotFound("Không tìm thấy lịch hẹn.");

    [HttpPost("lich-hen/{ma}/xac-nhan")]
    public async Task<LichHenLeTanItem> XacNhan(string ma, CancellationToken ct)
    {
        if (!await _repo.XacNhanAsync(ma, MaLeTan, BayGio, ct)) throw AppException.NotFound("Không tìm thấy lịch hẹn.");
        return (await _repo.TimLichHenAsync(ma, ct))!;
    }

    [HttpPost("lich-hen/{ma}/huy")]
    public async Task<LichHenLeTanItem> Huy(string ma, CancellationToken ct)
    {
        if (!await _repo.HuyAsync(ma, ct)) throw AppException.NotFound("Không tìm thấy lịch hẹn.");
        return (await _repo.TimLichHenAsync(ma, ct))!;
    }

    /// <summary>Tiếp nhận khách có hẹn (lịch Đã đặt thì xác nhận luôn, Q3).</summary>
    [HttpPost("lich-hen/{ma}/tiep-nhan")]
    public async Task<HoSoTiepNhanItem> TiepNhanCoHen(string ma, TiepNhanRequest req, CancellationToken ct)
    {
        var hoSo = await _repo.TiepNhanCoHenAsync(ma, MaLeTan, req.TrieuChung!.Trim(), BayGio, ct)
            ?? throw AppException.NotFound("Không tìm thấy lịch hẹn.");
        return (await _repo.TimHoSoAsync(hoSo, ct))!;
    }

    [HttpPost("tiep-nhan-vang-lai")]
    public async Task<HoSoTiepNhanItem> TiepNhanVangLai(TiepNhanVangLaiRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.TrieuChung)) throw AppException.BadRequest("Vui lòng nhập triệu chứng.");
        var hoSo = await _repo.TiepNhanVangLaiAsync(req, MaLeTan, BayGio, ct);
        return (await _repo.TimHoSoAsync(hoSo, ct))!;
    }

    [HttpGet("bac-si-dang-truc")]
    public Task<List<BacSiDangTrucItem>> BacSiDangTruc(CancellationToken ct) => _repo.BacSiDangTrucAsync(BayGio, ct);

    /// <summary>Hồ sơ khám đã lập trong ngày (mặc định hôm nay).</summary>
    [HttpGet("ho-so")]
    public Task<List<HoSoTiepNhanItem>> HoSo([FromQuery] DateOnly? ngay, CancellationToken ct)
        => _repo.HoSoTheoNgayAsync(ngay ?? DateOnly.FromDateTime(BayGio), ct);
}
