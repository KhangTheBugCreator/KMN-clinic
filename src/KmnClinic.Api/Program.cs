using System.Security.Claims;
using KmnClinic.Api.Data;
using KmnClinic.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---------- Cấu hình (bí mật đọc từ User Secrets / biến môi trường, không nằm trong appsettings.json) ----------
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
var signingKey = jwtOptions.SigningKey(); // dừng chương trình ngay nếu thiếu khóa

builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton<Db>();
builder.Services.AddScoped<AuthRepository>();

// ---------- Xác thực JWT + phân quyền theo vai trò ----------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimKmn.HoTen,
        };
        o.Events = new JwtBearerEvents
        {
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                await ExceptionMiddleware.WriteAsync(ctx.HttpContext, StatusCodes.Status401Unauthorized,
                    "Bạn cần chọn vai trò để tiếp tục.");
            },
            OnForbidden = ctx => ExceptionMiddleware.WriteAsync(ctx.HttpContext, StatusCodes.Status403Forbidden,
                "Bạn không có quyền thực hiện thao tác này."),
        };
    });
builder.Services.AddAuthorization();

// ---------- Controllers: lỗi kiểm tra dữ liệu trả về { message } giống các lỗi khác ----------
builder.Services.AddControllers().ConfigureApiBehaviorOptions(o =>
{
    o.InvalidModelStateResponseFactory = ctx =>
    {
        // Câu lỗi của từng trường trong Models đều là tiếng Việt. Lỗi đọc JSON hoặc câu mặc định
        // tiếng Anh của framework (lộ chi tiết kỹ thuật) được thay bằng một câu chung.
        var cacLoi = ctx.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
        var loi = cacLoi.FirstOrDefault(m => m.Any(c => c > 127)) ?? "Dữ liệu gửi lên không đúng định dạng.";
        return new BadRequestObjectResult(new { message = loi });
    };
});

var app = builder.Build();

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseDefaultFiles(new DefaultFilesOptions { DefaultFileNames = { "trang-chu.html" } });
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Đường dẫn /api không tồn tại: trả JSON thay vì trang HTML.
app.Map("/api/{**rest}", (HttpContext ctx) =>
    ExceptionMiddleware.WriteAsync(ctx, StatusCodes.Status404NotFound, "Không tìm thấy chức năng được yêu cầu."));

app.Run();

public partial class Program { }
