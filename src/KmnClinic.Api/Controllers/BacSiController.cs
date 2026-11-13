using KmnClinic.Api.Data;
using KmnClinic.Api.Infrastructure;
using KmnClinic.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KmnClinic.Api.Controllers;

/// <summary>
/// Bác sĩ (M7): hàng chờ, khám (triệu chứng, chẩn đoán, lời dặn, tiền công), kê đơn, hoàn tất tạo hóa đơn (Q4),
/// xem lịch sử chỉ đọc (Q13). Mã bác sĩ luôn lấy từ token; hồ sơ của bác sĩ khác trả 404.
/// </summary>
[ApiController]
[Route("api/bac-si")]
[Authorize(Roles = VaiTro.BacSi)]
public class BacSiController : ControllerBase
{
    private readonly BacSiRepository _repo;
    public BacSiController(BacSiRepository repo) => _repo = repo;

    private string MaBs => User.MaNguoiDung();
    private static AppException KhongThay() => AppException.NotFound("Không tìm thấy hồ sơ khám của bạn.");

    [HttpGet("tong-quan")]
    public Task<TongQuanBacSi> TongQuan(CancellationToken ct) => _repo.TongQuanAsync(MaBs, DateTime.Now, ct);

    [HttpGet("hang-cho")]
    public Task<List<HoSoBacSiItem>> HangCho(CancellationToken ct) => _repo.HangChoAsync(MaBs, ct);

    [HttpGet("ho-so")]
    public Task<TrangKetQua<HoSoBacSiItem>> LichSu([FromQuery] HoSoBacSiQuery q, CancellationToken ct)
    {
        if (q.TrangThai is { Length: > 0 } tt && !TrangThaiHoSo.TatCa.Contains(tt))
            throw AppException.BadRequest("Trạng thái hồ sơ không hợp lệ.");
        if (q.TuNgay is { } tu && q.DenNgay is { } den && tu > den)
            throw AppException.BadRequest("Từ ngày phải trước hoặc bằng đến ngày.");
        return _repo.LichSuAsync(MaBs, q, ct);
    }

    [HttpGet("ho-so/{ma}")]
    public async Task<HoSoChiTiet> ChiTiet(string ma, CancellationToken ct)
        => await _repo.ChiTietAsync(ma, MaBs, ct) ?? throw KhongThay();

    /// <summary>Các lần khám đã hoàn tất trước đó của cùng bệnh nhân (mọi bác sĩ), chỉ đọc.</summary>
    [HttpGet("ho-so/{ma}/lan-kham-truoc")]
    public async Task<List<LanKhamTruoc>> LanKhamTruoc(string ma, CancellationToken ct)
        => await _repo.LanKhamTruocAsync(ma, MaBs, ct) ?? throw KhongThay();

    /// <summary>Lưu nháp. Chẩn đoán được để trống.</summary>
    [HttpPut("ho-so/{ma}")]
    public Task<HoSoChiTiet> Luu(string ma, KhamBenhRequest req, CancellationToken ct)
    {
        KiemTra(req);
        return _repo.LuuAsync(ma, MaBs, req, ct);
    }

    [HttpPost("ho-so/{ma}/don-thuoc")]
    public Task<HoSoChiTiet> KeThuoc(string ma, KeThuocRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.LieuDung)) throw AppException.BadRequest("Vui lòng nhập liều dùng.");
        return _repo.KeThuocAsync(ma, MaBs, req, ct);
    }

    [HttpPut("ho-so/{ma}/don-thuoc/{maThuoc}")]
    public Task<HoSoChiTiet> SuaThuoc(string ma, string maThuoc, SuaThuocTrongDonRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.LieuDung)) throw AppException.BadRequest("Vui lòng nhập liều dùng.");
        return _repo.SuaThuocAsync(ma, maThuoc, MaBs, req, ct);
    }

    [HttpDelete("ho-so/{ma}/don-thuoc/{maThuoc}")]
    public Task<HoSoChiTiet> XoaThuoc(string ma, string maThuoc, CancellationToken ct)
        => _repo.XoaThuocAsync(ma, maThuoc, MaBs, ct);

    /// <summary>Hoàn tất khám: lập hóa đơn Chưa thanh toán và khóa hồ sơ (Q4). Bắt buộc có chẩn đoán (RB22).</summary>
    [HttpPost("ho-so/{ma}/hoan-tat")]
    public Task<HoSoChiTiet> HoanTat(string ma, KhamBenhRequest req, CancellationToken ct)
    {
        KiemTra(req);
        return _repo.HoanTatAsync(ma, MaBs, req, ct);
    }

    private static void KiemTra(KhamBenhRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.TrieuChung)) throw AppException.BadRequest("Vui lòng nhập triệu chứng.");
        if (req.TienCongKham is { } t && t != decimal.Truncate(t)) throw AppException.BadRequest("Tiền công khám tính theo đồng, không có số lẻ.");
    }
}
