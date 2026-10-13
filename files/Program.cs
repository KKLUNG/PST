using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.WebEncoders;
using BaseComponents;
using BlazorUserManagement.Components;
using BlazorUserManagement.Data;
using BlazorUserManagement.Services;

var builder = WebApplication.CreateBuilder(args);

// ========== 設定資料庫連接 ==========
// 使用 DbContextFactory：每次操作建立新的 DbContext，避免長時間連線共用同一個 DbContext
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }));

// ========== 註冊服務 ==========
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();

// 共用資料 (參數檔、地址)：提供給底層元件 BaseParameterSelect、BaseAddress
builder.Services.AddMemoryCache();
builder.Services.AddScoped<CommonDataService>();
builder.Services.AddScoped<IBaseParameterProvider>(sp => sp.GetRequiredService<CommonDataService>());
builder.Services.AddScoped<IBaseAddressProvider>(sp => sp.GetRequiredService<CommonDataService>());

// 測試資料暫存表 CMSData (各畫面以 PageName 區分，欄位對應見 Data/Cms/CmsMapper.cs)
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<BlazorUserManagement.Data.Cms.CmsDataService>();

// ========== 設定驗證與授權 (Cookie) ==========
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// ========== 加入 Blazor 元件 (互動式伺服器端) ==========
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ========== 加入共用底層元件 (含 MudBlazor) ==========
builder.Services.AddBaseComponents();

// ========== 中文直接輸出 ==========
// 預設會將中文編碼成 &#x4F7F; 形式（畫面正常但原始碼難以閱讀），改為允許所有 Unicode 字元；
// <、>、&、" 等 HTML 特殊字元仍會被編碼，不影響安全性
builder.Services.Configure<WebEncoderOptions>(options =>
{
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All);
});

// ========== 設定日誌 ==========
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}
else
{
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}

var app = builder.Build();

// ========== 檢查資料庫連線 ==========
// 資料庫由 Data/CreateDatabase.sql 建立，此處只確認能否連線
using (var scope = app.Services.CreateScope())
{
    using var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext();
    try
    {
        if (db.Database.CanConnect())
        {
            Console.WriteLine("✓ 資料庫連線成功");
        }
        else
        {
            Console.WriteLine("✗ 無法連線資料庫，請確認連接字串與 CreateDatabase.sql 是否已執行");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ 資料庫連線失敗: {ex.Message}");
    }
}

// ========== 設定 HTTP 管線 ==========
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// ========== 驗證與授權中間件 ==========
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// ========== 路由設定 ==========
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ========== 登出 ==========
// 由頁首的登出按鈕以 POST 表單送出 (含防偽權杖，[FromForm] 參數會觸發防偽驗證)
app.MapPost("/Account/Logout", async (HttpContext httpContext, [FromForm] string? returnUrl) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.LocalRedirect("/Account/Login");
});

// ========== 健康檢查端點 (選用) ==========
app.MapGet("/health", () => new { status = "ok", timestamp = DateTime.UtcNow });

// ========== 記錄應用啟動資訊 ==========
Console.WriteLine($"========================================");
Console.WriteLine($"🚀 使用者管理系統 (Blazor) 已啟動");
Console.WriteLine($"環境: {app.Environment.EnvironmentName}");
Console.WriteLine($"時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine($"========================================");

app.Run();
