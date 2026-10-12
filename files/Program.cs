using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using BlazorUserManagement;
using BlazorUserManagement.Data;
using BlazorUserManagement.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

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

// ========== 設定驗證與授權 ==========
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// ========== 加入 Razx Components & Routing ==========
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ========== 加入 MudBlazor 元件庫 ==========
builder.Services.AddMudServices();

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

// ========== 設定 CORS (如需要) ==========
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy
            .WithOrigins("https://localhost:7XXX")  // 改為您的實際 localhost 埠
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// ========== 資料庫遷移 ==========
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        // 應用待處理的遷移
        db.Database.Migrate();
        Console.WriteLine("✓ 資料庫遷移成功");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ 資料庫遷移失敗: {ex.Message}");
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
app.UseAntiforgery();

// ========== 驗證與授權中間件 ==========
app.UseAuthentication();
app.UseAuthorization();

// ========== 路由設定 ==========
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ========== 健康檢查端點 (選用) ==========
app.MapGet("/health", () => new { status = "ok", timestamp = DateTime.UtcNow });

// ========== 記錄應用啟動資訊 ==========
Console.WriteLine($"========================================");
Console.WriteLine($"🚀 Blazor User Management 應用已啟動");
Console.WriteLine($"環境: {app.Environment.EnvironmentName}");
Console.WriteLine($"時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine($"========================================");

app.Run();
