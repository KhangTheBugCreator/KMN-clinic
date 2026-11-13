using KmnClinic.Api.Data;
using KmnClinic.Api.Infrastructure;
using KmnClinic.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KmnClinic.Api.Controllers;

/// <summary>Kiểm tra và chuẩn hóa khoảng ngày: bỏ trống thì lấy tháng hiện tại; tối đa 366 ngày.</summary>
internal static class KhoangNgay
{
    public const int ToiDa = 366;

    public static (DateOnly Tu, DateOnly Den) ChuanHoa(KyQuery q)
    {
        var homNay = DateOnly.FromDateTime(DateTime.Now);
        var tu = q.TuNgay ?? new DateOnly(homNay.Year, homNay.Month, 1);
        var den = q.DenNgay ?? tu.AddMonths(1).AddDays(-1);
        if (tu > den) throw AppException.BadRequest("Từ ngày phải trước hoặc bằng đến ngày.");
        if (den.DayNumber - tu.DayNumber + 1 > ToiDa) throw AppException.BadRequest($"Khoảng thời gian tối đa {ToiDa} ngày.");
        return (tu, den);
    }
}

/// <summary>Thống kê cho dashboard quản trị (M9). Chỉ quản trị viên, chỉ đọc.</summary>
[ApiController]
[Route("api/thong-ke")]
[Authorize(Roles = VaiTro.QuanTriVien)]
public class ThongKeController : ControllerBase
{
    private readonly ThongKeRepository _repo;
    public ThongKeController(ThongKeRepository repo) => _repo = repo;

    [HttpGet("tong-quan")]
    public Task<TongQuanThongKe> TongQuan([FromQuery] KyQuery q, CancellationToken ct)
    {
        var (tu, den) = KhoangNgay.ChuanHoa(q);
        return _repo.TongQuanAsync(tu, den, ct);
    }

    [HttpGet("doanh-thu")]
    public Task<List<DiemDoanhThu>> DoanhThu([FromQuery] DoanhThuQuery q, CancellationToken ct)
    {
        var kieu = string.IsNullOrWhiteSpace(q.Kieu) ? "ngay" : q.Kieu.Trim();
        if (kieu is not ("ngay" or "thang")) throw AppException.BadRequest("Kiểu thống kê chỉ nhận ngay hoặc thang.");
        var (tu, den) = KhoangNgay.ChuanHoa(q);
        return _repo.DoanhThuAsync(tu, den, kieu == "thang", ct);
    }

    [HttpGet("lich-hen-theo-trang-thai")]
    public Task<Dictionary<string, int>> LichHen([FromQuery] KyQuery q, CancellationToken ct)
    {
        var (tu, den) = KhoangNgay.ChuanHoa(q);
        return _repo.LichHenTheoTrangThaiAsync(tu, den, ct);
    }

    [HttpGet("luot-kham")]
    public Task<List<DiemLuotKham>> LuotKham([FromQuery] KyQuery q, CancellationToken ct)
    {
        var (tu, den) = KhoangNgay.ChuanHoa(q);
        return _repo.LuotKhamAsync(tu, den, ct);
    }

    [HttpGet("thuoc-ke-nhieu")]
    public Task<List<ThuocKeNhieu>> ThuocKeNhieu([FromQuery] KyQuery q, [FromQuery] int top = 5, CancellationToken ct = default)
    {
        var (tu, den) = KhoangNgay.ChuanHoa(q);
        return _repo.ThuocKeNhieuAsync(tu, den, Math.Clamp(top, 1, 50), ct);
    }
}

/// <summary>Dữ liệu 5 báo cáo (M9); trình duyệt dựng file Excel bằng ExcelJS. Chỉ quản trị viên.</summary>
[ApiController]
[Route("api/bao-cao")]
[Authorize(Roles = VaiTro.QuanTriVien)]
public class BaoCaoController : ControllerBase
{
    private readonly ThongKeRepository _repo;
    public BaoCaoController(ThongKeRepository repo) => _repo = repo;

    private static BaoCao<T> Goi<T>(string tieuDe, DateOnly? tu, DateOnly? den, List<T> dong) => new(tieuDe, tu, den, DateTime.Now, dong);

    [HttpGet("doanh-thu")]
    public async Task<BaoCao<BcDoanhThuDong>> DoanhThu([FromQuery] KyQuery q, CancellationToken ct)
    {
        var (tu, den) = KhoangNgay.ChuanHoa(q);
        return Goi("Báo cáo doanh thu", tu, den, await _repo.BcDoanhThuAsync(tu, den, ct));
    }

    [HttpGet("luot-kham")]
    public async Task<BaoCao<BcLuotKhamDong>> LuotKham([FromQuery] KyQuery q, CancellationToken ct)
    {
        var (tu, den) = KhoangNgay.ChuanHoa(q);
        return Goi("Báo cáo lượt khám", tu, den, await _repo.BcLuotKhamAsync(tu, den, ct));
    }

    [HttpGet("lich-hen")]
    public async Task<BaoCao<BcLichHenDong>> LichHen([FromQuery] KyQuery q, CancellationToken ct)
    {
        var (tu, den) = KhoangNgay.ChuanHoa(q);
        return Goi("Báo cáo lịch hẹn", tu, den, await _repo.BcLichHenAsync(tu, den, ct));
    }

    /// <summary>Tồn kho tại thời điểm xuất, không theo khoảng ngày.</summary>
    [HttpGet("ton-kho")]
    public async Task<BaoCao<BcTonKhoDong>> TonKho(CancellationToken ct)
        => Goi("Báo cáo thuốc tồn kho", null, null, await _repo.BcTonKhoAsync(ct));

    [HttpGet("su-dung-thuoc")]
    public async Task<BaoCao<BcSuDungThuocDong>> SuDungThuoc([FromQuery] KyQuery q, CancellationToken ct)
    {
        var (tu, den) = KhoangNgay.ChuanHoa(q);
        return Goi("Báo cáo sử dụng thuốc", tu, den, await _repo.SuDungThuocAsync(tu, den, ct));
    }
}
