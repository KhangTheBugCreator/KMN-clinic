using KmnClinic.Api.Data;
using KmnClinic.Api.Infrastructure;
using KmnClinic.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KmnClinic.Api.Controllers;

/// <summary>
/// Khách hàng đặt, xem và tự hủy lịch hẹn của chính mình (M5). Mã khách luôn lấy từ token,
/// không nhận từ request, nên không thao tác được lịch hẹn của người khác.
/// </summary>
[ApiController]
[Route("api/lich-hen-cua-toi")]
[Authorize(Roles = VaiTro.KhachHang)]
public class LichHenCuaToiController : ControllerBase
{
    private readonly LichHenRepository _repo;
    public LichHenCuaToiController(LichHenRepository repo) => _repo = repo;

    private static DateTime BayGio => DateTime.Now;
    private string MaKh => User.MaNguoiDung();

    [HttpGet]
    public Task<List<LichHenCuaToiItem>> DanhSach(CancellationToken ct) => _repo.DanhSachAsync(MaKh, BayGio, ct);

    [HttpGet("{ma}")]
    public async Task<LichHenCuaToiItem> ChiTiet(string ma, CancellationToken ct)
        => await _repo.TimAsync(ma, MaKh, BayGio, ct) ?? throw AppException.NotFound("Không tìm thấy lịch hẹn.");

    [HttpPost]
    public async Task<LichHenCuaToiItem> DatLich(DatLichRequest req, CancellationToken ct)
    {
        var bayGio = BayGio;
        var ma = await _repo.DatLichAsync(MaKh, req, bayGio, ct);
        return (await _repo.TimAsync(ma, MaKh, bayGio, ct))!;
    }

    [HttpPost("{ma}/huy")]
    public async Task<LichHenCuaToiItem> Huy(string ma, CancellationToken ct)
    {
        if (!await _repo.HuyAsync(ma, MaKh, BayGio, ct)) throw AppException.NotFound("Không tìm thấy lịch hẹn.");
        return (await _repo.TimAsync(ma, MaKh, BayGio, ct))!;
    }
}
