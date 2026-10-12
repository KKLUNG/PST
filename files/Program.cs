using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using BlazorUserManagement.Data;
using BlazorUserManagement.Services;

var builder = WebApplication.CreateBuilder(args);

// ========== 設定資料庫連接 ==========
builder.Services.AddDbContext<ApplicationDbContext>(options =>
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

// ========== 加入 Razor Pages ==========
// 預設所有頁面都需要登入，僅登入頁與錯誤頁開放匿名存取
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Error");
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
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
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
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// ========== 驗證與授權中間件 ==========
app.UseAuthentication();
app.UseAuthorization();

// ========== 路由設定 ==========
app.MapRazorPages();

// ========== 健康檢查端點 (選用) ==========
app.MapGet("/health", () => new { status = "ok", timestamp = DateTime.UtcNow }).AllowAnonymous();

// ========== 記錄應用啟動資訊 ==========
Console.WriteLine($"========================================");
Console.WriteLine($"🚀 使用者管理系統 (Razor Pages) 已啟動");
Console.WriteLine($"環境: {app.Environment.EnvironmentName}");
Console.WriteLine($"時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine($"========================================");

app.Run();
