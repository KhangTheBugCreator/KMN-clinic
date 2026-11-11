using System.Net;
using System.Net.Http.Json;

namespace KmnClinic.Tests;

public class GioiHanDangNhapTests : IClassFixture<TestAppGioiHanThap>
{
    private readonly TestAppGioiHanThap _app;

    public GioiHanDangNhapTests(TestAppGioiHanThap app) => _app = app;

    [Fact]
    public async Task Thu_qua_nhieu_lan_bi_chan_429()
    {
        var c = _app.CreateClient();
        var codes = new List<HttpStatusCode>();
        for (var i = 0; i < 5; i++)
            codes.Add((await c.PostAsJsonAsync("/api/auth/nhan-vien/dang-nhap",
                new { soDienThoai = "0900000001", matKhau = "saimatkhau" })).StatusCode);

        Assert.Equal(new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized,
                             HttpStatusCode.TooManyRequests, HttpStatusCode.TooManyRequests }, codes);
    }
}
