using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using BlazorUserManagement.Data;
using BlazorUserManagement.Services;

namespace BlazorUserManagement.Tests;

/// <summary>
/// 使用者服務 (新增、修改、刪除、查詢)
/// </summary>
public class UserServiceTests
{
    private readonly TestDbContextFactory factory = new();
    private readonly UserService service;

    public UserServiceTests()
    {
        service = TestServices.CreateUserService(factory);
    }

    private Task<UserDto> CreateAsync(string username, string email, string? fullName = null, bool isActive = true) =>
        service.CreateUserAsync(new UserCreateEditRequest
        {
            Username = username,
            Email = email,
            FullName = fullName,
            IsActive = isActive,
            Password = "pass1234",
        });

    [Fact]
    public async Task 新增使用者_密碼加密儲存()
    {
        var user = await CreateAsync("test1", "test1@a.com", "測試");

        await using var db = factory.CreateDbContext();
        var saved = await db.Users.SingleAsync(u => u.UserId == user.UserId);
        Assert.NotEqual("pass1234", saved.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("pass1234", saved.PasswordHash));
    }

    [Fact]
    public async Task 新增停用的使用者_儲存為停用()
    {
        // 曾發生的問題：IsActive 設定資料庫預設值 true，false 會被存成 true
        var user = await CreateAsync("test1", "test1@a.com", isActive: false);

        await using var db = factory.CreateDbContext();
        Assert.False((await db.Users.SingleAsync(u => u.UserId == user.UserId)).IsActive);
    }

    [Fact]
    public async Task 帳號重複不可新增()
    {
        await CreateAsync("test1", "test1@a.com");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateAsync("test1", "other@a.com"));
        Assert.Contains("已存在", ex.Message);
    }

    [Fact]
    public async Task 電子郵件重複不可新增()
    {
        await CreateAsync("test1", "same@a.com");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateAsync("test2", "same@a.com"));
        Assert.Contains("已被使用", ex.Message);
    }

    [Fact]
    public async Task 新增時密碼必填()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateUserAsync(new UserCreateEditRequest
        {
            Username = "test1",
            Email = "test1@a.com",
        }));
    }

    [Fact]
    public async Task 修改時密碼留空維持原密碼()
    {
        var user = await CreateAsync("test1", "test1@a.com");
        string originalHash;
        await using (var db = factory.CreateDbContext())
        {
            originalHash = (await db.Users.SingleAsync(u => u.UserId == user.UserId)).PasswordHash;
        }

        await service.UpdateUserAsync(user.UserId, new UserCreateEditRequest
        {
            Username = "test1",
            Email = "new@a.com",
            FullName = "已修改",
            IsActive = true,
            Password = null,
        });

        await using var check = factory.CreateDbContext();
        var saved = await check.Users.SingleAsync(u => u.UserId == user.UserId);
        Assert.Equal(originalHash, saved.PasswordHash);
        Assert.Equal("new@a.com", saved.Email);
        Assert.Equal("已修改", saved.FullName);
        Assert.NotNull(saved.UpdatedDate);
    }

    [Fact]
    public async Task 修改時輸入新密碼則更新()
    {
        var user = await CreateAsync("test1", "test1@a.com");

        await service.UpdateUserAsync(user.UserId, new UserCreateEditRequest
        {
            Username = "test1",
            Email = "test1@a.com",
            IsActive = true,
            Password = "newpass99",
        });

        await using var db = factory.CreateDbContext();
        Assert.True(BCrypt.Net.BCrypt.Verify("newpass99", (await db.Users.SingleAsync()).PasswordHash));
    }

    [Fact]
    public async Task 刪除使用者()
    {
        var user = await CreateAsync("test1", "test1@a.com");

        await service.DeleteUserAsync(user.UserId);

        await using var db = factory.CreateDbContext();
        Assert.False(await db.Users.AnyAsync());
    }

    [Fact]
    public async Task 刪除不存在的使用者()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteUserAsync(999));
    }

    [Fact]
    public async Task 多條件查詢()
    {
        await CreateAsync("admin", "admin@company.com", "系統管理員");
        await CreateAsync("user1", "user1@company.com", "李使用者");
        await CreateAsync("user2", "user2@company.com", null, isActive: false);   // 姓名為 null

        Assert.Equal(3, (await service.QueryUsersAsync(new UserQuery())).Count);
        Assert.Equal(["user1", "user2"], (await service.QueryUsersAsync(new UserQuery { Username = "user" })).Select(u => u.Username));
        Assert.Equal(["user1"], (await service.QueryUsersAsync(new UserQuery { FullName = "李" })).Select(u => u.Username));
        Assert.Equal(["user2"], (await service.QueryUsersAsync(new UserQuery { IsActive = false })).Select(u => u.Username));
        Assert.Equal(["admin"], (await service.QueryUsersAsync(new UserQuery { Email = "admin@", IsActive = true })).Select(u => u.Username));
        Assert.Empty(await service.QueryUsersAsync(new UserQuery { Username = "none" }));
    }

    [Fact]
    public async Task 查詢條件前後空白會忽略()
    {
        await CreateAsync("user1", "user1@company.com", "李使用者");

        Assert.Single(await service.QueryUsersAsync(new UserQuery { Username = "  user1  " }));
    }
}

/// <summary>
/// 登入驗證
/// </summary>
public class AuthServiceTests
{
    private readonly TestDbContextFactory factory = new();

    private async Task SeedAsync(bool isActive = true)
    {
        await using var db = factory.CreateDbContext();
        db.Users.Add(new User
        {
            Username = "admin",
            Email = "admin@company.com",
            FullName = "系統管理員",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            IsActive = isActive,
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task 帳號密碼正確登入成功並記錄登入時間()
    {
        await SeedAsync();
        var auth = TestServices.CreateAuthService(factory);

        var result = await auth.LoginAsync(new LoginRequest { Username = "admin", Password = "admin123" });

        Assert.True(result.Success);
        Assert.Equal("系統管理員", result.User?.FullName);
        Assert.False(string.IsNullOrEmpty(result.Token));
        await using var db = factory.CreateDbContext();
        Assert.NotNull((await db.Users.SingleAsync()).LastLoginDate);
    }

    [Fact]
    public async Task 密碼錯誤()
    {
        await SeedAsync();

        var result = await TestServices.CreateAuthService(factory).LoginAsync(new LoginRequest { Username = "admin", Password = "wrong" });

        Assert.False(result.Success);
        Assert.Equal("使用者名稱或密碼不正確", result.Message);
    }

    [Fact]
    public async Task 帳號不存在與密碼錯誤顯示相同訊息()
    {
        // 避免從訊息差異推測帳號是否存在
        await SeedAsync();

        var result = await TestServices.CreateAuthService(factory).LoginAsync(new LoginRequest { Username = "nobody", Password = "admin123" });

        Assert.False(result.Success);
        Assert.Equal("使用者名稱或密碼不正確", result.Message);
    }

    [Fact]
    public async Task 停用的帳號不可登入()
    {
        await SeedAsync(isActive: false);

        var result = await TestServices.CreateAuthService(factory).LoginAsync(new LoginRequest { Username = "admin", Password = "admin123" });

        Assert.False(result.Success);
        Assert.Contains("停用", result.Message);
    }

    [Fact]
    public void 測試帳號的密碼雜湊確實是admin123()
    {
        // CreateDatabase.sql 中的雜湊 (曾發生雜湊與說明不符導致無法登入)
        const string hashInScript = "$2a$11$aC.OdDM52jzAE2yLZSaI3OzRQy7TMBJwDU7ul6PluuKre6Nn4w0ca";
        Assert.True(BCrypt.Net.BCrypt.Verify("admin123", hashInScript));
    }
}

/// <summary>
/// 共用資料服務 (參數檔、地址)
/// </summary>
public class CommonDataServiceTests
{
    private readonly TestDbContextFactory factory = new();
    private readonly CommonDataService service;

    public CommonDataServiceTests()
    {
        service = new CommonDataService(factory, new MemoryCache(new MemoryCacheOptions()));

        using var db = factory.CreateDbContext();
        db.Parameters.AddRange(
            new Parameter { SetItem = "UNIT", SetType = "B", SetValue = "台中郵局", SortOrder = 2 },
            new Parameter { SetItem = "UNIT", SetType = "A", SetValue = "台北郵局", SortOrder = 1 },
            new Parameter { SetItem = "UNIT", SetType = "D", SetValue = "花蓮郵局", SortOrder = 4, IsDeleted = true },
            new Parameter { SetItem = "PAY_TYPE", SetType = "1", SetValue = "現金", SortOrder = 1 });
        db.AddressAreas.AddRange(
            new AddressArea { County = "新北市", District = "板橋區", ZipCode = "220", SortOrder = 201 },
            new AddressArea { County = "臺北市", District = "大安區", ZipCode = "106", SortOrder = 105 },
            new AddressArea { County = "臺北市", District = "中正區", ZipCode = "100", SortOrder = 101 });
        db.SaveChanges();
    }

    [Fact]
    public async Task 參數依排序且排除已刪除()
    {
        var options = await service.GetOptionsAsync("UNIT");

        Assert.Equal(["A", "B"], options.Select(o => o.Value));
        Assert.Equal("台北郵局", options[0].Text);
    }

    [Fact]
    public async Task 不存在的參數群組回傳空清單()
    {
        Assert.Empty(await service.GetOptionsAsync("NONE"));
    }

    [Fact]
    public async Task 參數讀取後快取()
    {
        await service.GetOptionsAsync("PAY_TYPE");
        await using (var db = factory.CreateDbContext())
        {
            db.Parameters.Add(new Parameter { SetItem = "PAY_TYPE", SetType = "2", SetValue = "信用卡", SortOrder = 2 });
            await db.SaveChangesAsync();
        }

        // 快取期間不會重新查詢資料庫
        Assert.Single(await service.GetOptionsAsync("PAY_TYPE"));
    }

    [Fact]
    public async Task 縣市依排序()
    {
        Assert.Equal(["臺北市", "新北市"], await service.GetCountiesAsync());
    }

    [Fact]
    public async Task 鄉鎮市區與郵遞區號()
    {
        var districts = await service.GetDistrictsAsync("臺北市");

        Assert.Equal(["中正區", "大安區"], districts.Select(d => d.District));
        Assert.Equal("106", districts[1].ZipCode);
    }
}

/// <summary>
/// 使用者表單模型驗證
/// </summary>
public class UserModelValidationTests
{
    [Fact]
    public void 編輯時密碼與姓名可留空()
    {
        // 曾發生的問題：不可為 null 的 string 會被當成必填
        var request = new UserCreateEditRequest { Username = "user1", Email = "user1@a.com", Password = null, FullName = null };

        Assert.Empty(ValidationHelper.Validate(request));
    }

    [Theory]
    [InlineData("ab", "使用者名稱長度必須介於3-50個字元")]
    [InlineData("", "使用者名稱不可為空")]
    public void 帳號長度(string username, string message)
    {
        var request = new UserCreateEditRequest { Username = username, Email = "a@a.com" };

        Assert.Contains(ValidationHelper.Validate(request), e => e.Message == message);
    }

    [Fact]
    public void 電子郵件格式()
    {
        var request = new UserCreateEditRequest { Username = "user1", Email = "bad-email" };

        Assert.Contains(ValidationHelper.Validate(request), e => e.Message == "電子郵件格式不正確");
    }

    [Fact]
    public void 密碼至少6個字元()
    {
        var request = new UserCreateEditRequest { Username = "user1", Email = "a@a.com", Password = "12345" };

        Assert.Contains(ValidationHelper.Validate(request), e => e.Message == "密碼長度必須至少6個字元");
    }
}
