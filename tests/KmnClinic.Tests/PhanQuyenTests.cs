using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using KmnClinic.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace KmnClinic.Tests;

/// <summary>
/// M10: phân quyền cho TỪNG API, tự liệt kê bằng reflection nên API mới thêm sau này cũng được kiểm tra.
/// 1) Mọi action phải khai báo rõ [Authorize] hoặc [AllowAnonymous]; chỉ API công khai, giả lập vai trò, trạng thái hệ thống được mở.
/// 2) Gọi thật từng API: không token → 401, sai vai trò → 403, đúng vai trò → không bị chặn (chỉ gọi GET để không đổi dữ liệu).
/// </summary>
public class PhanQuyenTests : IClassFixture<TestApp>
{
    private readonly TestApp _app;
    public PhanQuyenTests(TestApp app) => _app = app;

    private static readonly string[] TatCaVaiTro = { VaiTro.QuanTriVien, VaiTro.LeTan, VaiTro.BacSi, VaiTro.ThuNgan, VaiTro.KhachHang };
    private static readonly Dictionary<string, (string Loai, string Ma)> NguoiMau = new()
    {
        [VaiTro.QuanTriVien] = ("NV", "NV0001"), [VaiTro.LeTan] = ("NV", "NV0002"), [VaiTro.BacSi] = ("NV", "NV0003"),
        [VaiTro.ThuNgan] = ("NV", "NV0005"), [VaiTro.KhachHang] = ("KH", "KH0001"),
    };
    private static readonly string[] ControllerCongKhai = { "CongKhaiController", "GiaLapController", "HeThongController" };

    public record ApiInfo(string Controller, string Method, string Url, bool AnDanh, string[] VaiTroDuocPhep);

    /// <summary>Mọi action của API (không tính controller chỉ có trong kiểm thử), kèm vai trò được phép thật sự (giao của các [Authorize]).</summary>
    public static List<ApiInfo> TatCaApi()
    {
        var ds = new List<ApiInfo>();
        foreach (var ctl in typeof(Program).Assembly.GetTypes().Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract))
        {
            var route = ctl.GetCustomAttribute<RouteAttribute>()!.Template;
            foreach (var m in ctl.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                var http = m.GetCustomAttributes<HttpMethodAttribute>().FirstOrDefault();
                if (http is null) continue;
                var url = "/" + route + (string.IsNullOrEmpty(http.Template) ? "" : "/" + http.Template);
                url = System.Text.RegularExpressions.Regex.Replace(url, @"\{[^}]+\}", "KHONGCO01");
                var anDanh = m.GetCustomAttribute<AllowAnonymousAttribute>() is not null || ctl.GetCustomAttribute<AllowAnonymousAttribute>() is not null;
                var auth = ctl.GetCustomAttributes<AuthorizeAttribute>().Concat(m.GetCustomAttributes<AuthorizeAttribute>()).ToList();
                IEnumerable<string> duoc = TatCaVaiTro;
                foreach (var a in auth.Where(a => !string.IsNullOrEmpty(a.Roles)))
                    duoc = duoc.Intersect(a.Roles!.Split(','));
                ds.Add(new(ctl.Name, http.HttpMethods.First(), url, anDanh, auth.Count == 0 ? Array.Empty<string>() : duoc.ToArray()));
            }
        }
        return ds;
    }

    [Fact]
    public void Moi_api_deu_khai_bao_quyen_ro_rang()
    {
        var apis = TatCaApi();
        Assert.True(apis.Count > 60, $"Chỉ thấy {apis.Count} API, reflection có vấn đề?");
        foreach (var a in apis)
        {
            if (a.AnDanh)
                Assert.True(ControllerCongKhai.Contains(a.Controller), $"{a.Method} {a.Url} mở cho người chưa đăng nhập.");
            else
                Assert.True(a.VaiTroDuocPhep.Length > 0, $"{a.Method} {a.Url} thiếu [Authorize] hoặc không vai trò nào dùng được.");
        }
    }

    /// <summary>
    /// Ma trận phân quyền đã chốt: mỗi API → vai trò được gọi (QTV quản trị viên, LT lễ tân, BS bác sĩ, TN thu ngân, KH khách hàng, * không cần đăng nhập).
    /// Thêm, bớt hay đổi quyền một API thì test này đỏ cho tới khi sửa ma trận, để mọi thay đổi quyền đều có chủ đích.
    /// </summary>
    private static readonly Dictionary<string, string> MaTran = new()
    {
        ["GET /api/auth/me"] = "QTV LT BS TN KH",
        ["GET /api/bac-si/hang-cho"] = "BS",
        ["GET /api/bac-si/ho-so"] = "BS",
        ["GET /api/bac-si/ho-so/KHONGCO01"] = "BS",
        ["PUT /api/bac-si/ho-so/KHONGCO01"] = "BS",
        ["POST /api/bac-si/ho-so/KHONGCO01/don-thuoc"] = "BS",
        ["PUT /api/bac-si/ho-so/KHONGCO01/don-thuoc/KHONGCO01"] = "BS",
        ["DELETE /api/bac-si/ho-so/KHONGCO01/don-thuoc/KHONGCO01"] = "BS",
        ["POST /api/bac-si/ho-so/KHONGCO01/hoan-tat"] = "BS",
        ["GET /api/bac-si/ho-so/KHONGCO01/lan-kham-truoc"] = "BS",
        ["GET /api/bac-si/lich-hen"] = "BS",
        ["GET /api/bac-si/tong-quan"] = "BS",
        ["GET /api/bao-cao/doanh-thu"] = "QTV",
        ["GET /api/bao-cao/lich-hen"] = "QTV",
        ["GET /api/bao-cao/luot-kham"] = "QTV",
        ["GET /api/bao-cao/su-dung-thuoc"] = "QTV",
        ["GET /api/bao-cao/ton-kho"] = "QTV",
        ["GET /api/ca"] = "QTV LT BS TN",
        ["POST /api/ca"] = "QTV",
        ["GET /api/ca/KHONGCO01"] = "QTV LT BS TN",
        ["PUT /api/ca/KHONGCO01"] = "QTV",
        ["DELETE /api/ca/KHONGCO01"] = "QTV",
        ["GET /api/cong-khai/bac-si"] = "*",
        ["GET /api/cong-khai/bac-si/KHONGCO01"] = "*",
        ["GET /api/cong-khai/khoa"] = "*",
        ["GET /api/cong-khai/lich-kham"] = "*",
        ["GET /api/cong-khai/tong-quan"] = "*",
        ["GET /api/gia-lap/khach-hang"] = "*",
        ["POST /api/gia-lap/khach-hang"] = "*",
        ["GET /api/gia-lap/nhan-vien"] = "*",
        ["POST /api/gia-lap/vao-vai-tro"] = "*",
        ["GET /api/he-thong/trang-thai"] = "*",
        ["GET /api/khach-hang"] = "QTV LT",
        ["POST /api/khach-hang"] = "QTV LT",
        ["GET /api/khach-hang/KHONGCO01"] = "QTV LT",
        ["PUT /api/khach-hang/KHONGCO01"] = "QTV LT",
        ["DELETE /api/khach-hang/KHONGCO01"] = "QTV",
        ["GET /api/khoa"] = "QTV LT BS TN",
        ["POST /api/khoa"] = "QTV",
        ["GET /api/khoa/KHONGCO01"] = "QTV LT BS TN",
        ["PUT /api/khoa/KHONGCO01"] = "QTV",
        ["DELETE /api/khoa/KHONGCO01"] = "QTV",
        ["GET /api/le-tan/bac-si-dang-truc"] = "LT",
        ["GET /api/le-tan/ho-so"] = "LT",
        ["GET /api/le-tan/lich-hen"] = "LT",
        ["GET /api/le-tan/lich-hen/KHONGCO01"] = "LT",
        ["POST /api/le-tan/lich-hen/KHONGCO01/huy"] = "LT",
        ["POST /api/le-tan/lich-hen/KHONGCO01/tiep-nhan"] = "LT",
        ["POST /api/le-tan/lich-hen/KHONGCO01/xac-nhan"] = "LT",
        ["POST /api/le-tan/tiep-nhan-vang-lai"] = "LT",
        ["GET /api/le-tan/tong-quan"] = "LT",
        ["GET /api/lich-hen-cua-toi"] = "KH",
        ["POST /api/lich-hen-cua-toi"] = "KH",
        ["GET /api/lich-hen-cua-toi/KHONGCO01"] = "KH",
        ["POST /api/lich-hen-cua-toi/KHONGCO01/huy"] = "KH",
        ["GET /api/lich-lam-viec"] = "QTV LT BS TN",
        ["POST /api/lich-lam-viec"] = "QTV",
        ["GET /api/lich-lam-viec/bac-si"] = "QTV LT BS TN",
        ["POST /api/lich-lam-viec/hang-loat"] = "QTV",
        ["GET /api/lich-lam-viec/KHONGCO01"] = "QTV LT BS TN",
        ["PUT /api/lich-lam-viec/KHONGCO01"] = "QTV",
        ["DELETE /api/lich-lam-viec/KHONGCO01"] = "QTV",
        ["GET /api/nhan-vien"] = "QTV",
        ["POST /api/nhan-vien"] = "QTV",
        ["GET /api/nhan-vien/KHONGCO01"] = "QTV",
        ["PUT /api/nhan-vien/KHONGCO01"] = "QTV",
        ["DELETE /api/nhan-vien/KHONGCO01"] = "QTV",
        ["GET /api/phong"] = "QTV LT BS TN",
        ["POST /api/phong"] = "QTV",
        ["GET /api/phong/KHONGCO01"] = "QTV LT BS TN",
        ["PUT /api/phong/KHONGCO01"] = "QTV",
        ["DELETE /api/phong/KHONGCO01"] = "QTV",
        ["GET /api/phong/KHONGCO01/lich-su-dung"] = "QTV",
        ["GET /api/quan-tri/ho-so"] = "QTV",
        ["GET /api/quan-tri/ho-so/KHONGCO01"] = "QTV",
        ["GET /api/quan-tri/hoa-don"] = "QTV",
        ["GET /api/quan-tri/hoa-don/KHONGCO01"] = "QTV",
        ["GET /api/quan-tri/lich-hen"] = "QTV",
        ["GET /api/quan-tri/lich-hen/KHONGCO01"] = "QTV",
        ["GET /api/thong-ke/doanh-thu"] = "QTV",
        ["GET /api/thong-ke/lich-hen-theo-trang-thai"] = "QTV",
        ["GET /api/thong-ke/luot-kham"] = "QTV",
        ["GET /api/thong-ke/thuoc-ke-nhieu"] = "QTV",
        ["GET /api/thong-ke/tong-quan"] = "QTV",
        ["GET /api/thu-ngan/hoa-don"] = "TN",
        ["GET /api/thu-ngan/hoa-don/KHONGCO01"] = "TN",
        ["POST /api/thu-ngan/hoa-don/KHONGCO01/thanh-toan"] = "TN",
        ["GET /api/thu-ngan/tong-quan"] = "TN",
        ["GET /api/thuoc"] = "QTV BS TN",
        ["POST /api/thuoc"] = "QTV",
        ["GET /api/thuoc/KHONGCO01"] = "QTV BS TN",
        ["PUT /api/thuoc/KHONGCO01"] = "QTV",
        ["GET /api/thuoc/sap-het"] = "QTV BS TN",
    };

    private static readonly Dictionary<string, string> VietTat = new()
    {
        ["QTV"] = VaiTro.QuanTriVien, ["LT"] = VaiTro.LeTan, ["BS"] = VaiTro.BacSi, ["TN"] = VaiTro.ThuNgan, ["KH"] = VaiTro.KhachHang,
    };

    [Fact]
    public void Quyen_tung_api_dung_ma_tran_da_chot()
    {
        var thuc = TatCaApi().ToDictionary(a => $"{a.Method} {a.Url}");
        var loi = new List<string>();
        foreach (var (api, a) in thuc)
        {
            if (!MaTran.TryGetValue(api, out var mongDoi)) { loi.Add($"API mới chưa có trong ma trận: {api}"); continue; }
            var coThuc = a.AnDanh ? "*" : string.Join(" ", a.VaiTroDuocPhep.OrderBy(x => x));
            var coMongDoi = mongDoi == "*" ? "*" : string.Join(" ", mongDoi.Split(' ').Select(x => VietTat[x]).OrderBy(x => x));
            if (coThuc != coMongDoi) loi.Add($"{api}: đang cho {coThuc}, ma trận ghi {coMongDoi}");
        }
        loi.AddRange(MaTran.Keys.Except(thuc.Keys).Select(k => $"Ma trận có API không còn tồn tại: {k}"));
        Assert.True(loi.Count == 0, string.Join("\n", loi));
    }

    [Fact]
    public void Api_ghi_du_lieu_nghiep_vu_dung_vai_tro()
    {
        var apis = TatCaApi().Where(a => a.Method != "GET" && !a.AnDanh).ToList();
        // Mỗi thao tác ghi chỉ thuộc đúng vai trò nghiệp vụ, khách hàng không bao giờ ghi được dữ liệu nội bộ
        foreach (var a in apis.Where(a => !a.Url.StartsWith("/api/lich-hen-cua-toi")))
            Assert.DoesNotContain(VaiTro.KhachHang, a.VaiTroDuocPhep);
        Assert.All(apis.Where(a => a.Url.StartsWith("/api/thu-ngan")), a => Assert.Equal(new[] { VaiTro.ThuNgan }, a.VaiTroDuocPhep));
        Assert.All(apis.Where(a => a.Url.StartsWith("/api/bac-si")), a => Assert.Equal(new[] { VaiTro.BacSi }, a.VaiTroDuocPhep));
        Assert.All(apis.Where(a => a.Url.StartsWith("/api/le-tan")), a => Assert.Equal(new[] { VaiTro.LeTan }, a.VaiTroDuocPhep));
        Assert.All(apis.Where(a => a.Url is "/api/thuoc" or "/api/thuoc/KHONGCO01" || a.Url.StartsWith("/api/nhan-vien")
                || a.Url.StartsWith("/api/khoa") || a.Url.StartsWith("/api/phong") || a.Url.StartsWith("/api/ca")),
            a => Assert.Equal(new[] { VaiTro.QuanTriVien }, a.VaiTroDuocPhep));
        // Quản trị viên chỉ đọc vận hành
        Assert.All(TatCaApi().Where(a => a.Url.StartsWith("/api/quan-tri")), a => Assert.Equal("GET", a.Method));
    }

    private async Task<HttpClient> Client(string? vaiTro)
    {
        var c = _app.CreateClient();
        if (vaiTro is null) return c;
        var (loai, ma) = NguoiMau[vaiTro];
        var res = await c.PostAsJsonAsync("/api/gia-lap/vao-vai-tro", new { loai, ma });
        var token = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        c = _app.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    private static HttpRequestMessage YeuCau(ApiInfo a)
    {
        var req = new HttpRequestMessage(new HttpMethod(a.Method), a.Url);
        if (a.Method != "GET" && a.Method != "DELETE") req.Content = JsonContent.Create(new { });
        return req;
    }

    [Fact]
    public async Task Goi_that_tung_api_chan_dung_401_403()
    {
        var apis = TatCaApi().Where(a => !a.AnDanh).ToList();
        var clients = new Dictionary<string, HttpClient>();
        foreach (var v in TatCaVaiTro) clients[v] = await Client(v);
        var anDanh = await Client(null);
        var loi = new List<string>();

        foreach (var a in apis)
        {
            var s = (await anDanh.SendAsync(YeuCau(a))).StatusCode;
            if (s != HttpStatusCode.Unauthorized) loi.Add($"Không token {a.Method} {a.Url} → {(int)s}");
            foreach (var v in TatCaVaiTro)
            {
                var duoc = a.VaiTroDuocPhep.Contains(v);
                if (duoc && a.Method != "GET") continue; // không gọi thao tác ghi bằng vai trò hợp lệ
                s = (await clients[v].SendAsync(YeuCau(a))).StatusCode;
                if (!duoc && s != HttpStatusCode.Forbidden) loi.Add($"{v} {a.Method} {a.Url} → {(int)s}, cần 403");
                if (duoc && s is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized or HttpStatusCode.InternalServerError)
                    loi.Add($"{v} {a.Method} {a.Url} → {(int)s}, vai trò hợp lệ bị chặn hoặc lỗi");
            }
        }
        Assert.True(loi.Count == 0, string.Join("\n", loi));
    }
}
