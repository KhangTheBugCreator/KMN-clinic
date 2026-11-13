using KmnClinic.Api.Data;
using KmnClinic.Api.Infrastructure;
using KmnClinic.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KmnClinic.Api.Controllers;

/// <summary>
/// Thu ngân (M8): xem hóa đơn, xác nhận đã thu tiền. Mã thu ngân lấy từ token (RB32 ghi đúng người thu).
/// Không có API sửa, xóa hay hủy hóa đơn để giữ lịch sử thanh toán.
/// </summary>
[ApiController]
[Route("api/thu-ngan")]
[Authorize(Roles = VaiTro.ThuNgan)]
public class ThuNganController : ControllerBase
{
    private readonly ThuNganRepository _repo;
    public ThuNganController(ThuNganRepository repo) => _repo = repo;

    private string MaThuNgan => User.MaNguoiDung();

    [HttpGet("tong-quan")]
    public Task<TongQuanThuNgan> TongQuan(CancellationToken ct) => _repo.TongQuanAsync(MaThuNgan, DateTime.Now, ct);

    [HttpGet("hoa-don")]
    public Task<TrangKetQua<HoaDonItem>> DanhSach([FromQuery] HoaDonQuery tk, CancellationToken ct)
    {
        if (tk.TrangThai is { Length: > 0 } tt && !TrangThaiHoaDon.TatCa.Contains(tt))
            throw AppException.BadRequest("Trạng thái hóa đơn không hợp lệ.");
        if (tk.TuNgay is { } tu && tk.DenNgay is { } den && tu > den)
            throw AppException.BadRequest("Từ ngày phải trước hoặc bằng đến ngày.");
        return _repo.DanhSachAsync(tk, MaThuNgan, ct);
    }

    [HttpGet("hoa-don/{ma}")]
    public async Task<HoaDonChiTiet> ChiTiet(string ma, CancellationToken ct)
        => await _repo.ChiTietAsync(ma, ct) ?? throw AppException.NotFound("Không tìm thấy hóa đơn.");

    /// <summary>Chưa thanh toán → Đã thanh toán, ghi giờ thu và thu ngân (RB31, RB32), trừ kho (RB33).</summary>
    [HttpPost("hoa-don/{ma}/thanh-toan")]
    public async Task<HoaDonChiTiet> ThanhToan(string ma, CancellationToken ct)
        => await _repo.ThanhToanAsync(ma, MaThuNgan, DateTime.Now, ct) ?? throw AppException.NotFound("Không tìm thấy hóa đơn.");
}
