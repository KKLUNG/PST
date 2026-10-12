using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using BlazorUserManagement.Data;
using BlazorUserManagement.Services;

namespace BlazorUserManagement.Tests;

/// <summary>
/// 記憶體資料庫的 DbContextFactory：每個測試使用獨立的資料庫名稱，彼此不互相影響
/// </summary>
public sealed class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
{
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public TestDbContextFactory()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"test-{Guid.NewGuid()}")
            .Options;
    }

    public ApplicationDbContext CreateDbContext() => new(_options);
}

public static class TestServices
{
    /// <summary>JWT 設定 (AuthService 產生 Token 用)</summary>
    public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "unit-test-secret-key-at-least-32-characters-long",
            ["Jwt:Issuer"] = "UnitTest",
            ["Jwt:Audience"] = "UnitTest",
            ["Jwt:ExpirationMinutes"] = "60",
        })
        .Build();

    public static AuthService CreateAuthService(TestDbContextFactory factory) =>
        new(Configuration, factory, NullLogger<AuthService>.Instance);

    public static UserService CreateUserService(TestDbContextFactory factory) =>
        new(factory, NullLogger<UserService>.Instance, CreateAuthService(factory));
}

public static class ValidationHelper
{
    /// <summary>
    /// 以 DataAnnotations 驗證物件 (含 IValidatableObject)，回傳 (欄位, 訊息) 清單
    /// </summary>
    public static List<(string Member, string Message)> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results
            .SelectMany(r => r.MemberNames.DefaultIfEmpty(string.Empty).Select(m => (m, r.ErrorMessage ?? string.Empty)))
            .ToList();
    }
}
