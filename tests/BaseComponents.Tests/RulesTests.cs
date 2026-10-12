using System.ComponentModel.DataAnnotations;
using BaseComponents;

namespace BaseComponents.Tests;

/// <summary>
/// 數字格式化與解析 (BaseNumeric 千分位)
/// </summary>
public class BaseNumberFormatTests
{
    [Theory]
    [InlineData(1234567.5, 2, true, false, "1,234,567.5")]
    [InlineData(1234567.5, 2, true, true, "1,234,567.50")]
    [InlineData(1234567.5, 2, false, false, "1234567.5")]
    [InlineData(1234.567, 2, true, false, "1,234.57")]
    [InlineData(-1234, 0, true, false, "-1,234")]
    [InlineData(0, 2, true, true, "0.00")]
    [InlineData(999, 0, true, false, "999")]
    public void Format_顯示千分位與小數位數(double value, int decimals, bool thousand, bool fixedDecimals, string expected)
    {
        Assert.Equal(expected, BaseNumberFormat.Format((decimal)value, decimals, thousand, fixedDecimals));
    }

    [Fact]
    public void Format_null回傳空字串()
    {
        Assert.Equal(string.Empty, BaseNumberFormat.Format(null, 2));
    }

    [Theory]
    [InlineData(1234567.5, "1234567.5")]
    [InlineData(12.50, "12.5")]
    [InlineData(100, "100")]
    [InlineData(-3.25, "-3.25")]
    public void FormatRaw_編輯時不含千分位且去除尾端0(double value, string expected)
    {
        Assert.Equal(expected, BaseNumberFormat.FormatRaw((decimal)value));
    }

    [Theory]
    [InlineData("1,234,567.5", 1234567.5)]
    [InlineData(" 1234 ", 1234)]
    [InlineData("-12.5", -12.5)]
    [InlineData("0", 0)]
    public void TryParse_可解析含千分位的文字(string text, double expected)
    {
        Assert.True(BaseNumberFormat.TryParse(text, out var value));
        Assert.Equal((decimal)expected, value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TryParse_空白回傳null(string? text)
    {
        Assert.True(BaseNumberFormat.TryParse(text, out var value));
        Assert.Null(value);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1.2.3")]
    [InlineData("-")]
    public void TryParse_無法解析回傳false(string text)
    {
        Assert.False(BaseNumberFormat.TryParse(text, out _));
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("123", true)]
    [InlineData("123.45", true)]
    [InlineData("123.", true)]
    [InlineData("1234", false)]      // 整數超過 3 位
    [InlineData("12.345", false)]    // 小數超過 2 位
    [InlineData("-12", false)]       // 不允許負數
    [InlineData("12a", false)]
    [InlineData("1,234", false)]     // 輸入時不可打逗號
    public void IsAllowedInput_整數3位小數2位不可負數(string text, bool expected)
    {
        Assert.Equal(expected, BaseNumberFormat.IsAllowedInput(text, integerDigits: 3, decimalDigits: 2, allowNegative: false));
    }

    [Theory]
    [InlineData("-", true)]
    [InlineData("-12", true)]
    [InlineData("12.5", false)]      // 小數 0 位
    [InlineData("12.", false)]
    public void IsAllowedInput_允許負數且無小數(string text, bool expected)
    {
        Assert.Equal(expected, BaseNumberFormat.IsAllowedInput(text, integerDigits: 5, decimalDigits: 0, allowNegative: true));
    }

    [Theory]
    [InlineData("1.005", 2, null, null, "1.01")]   // 四捨五入 (非銀行家捨入)
    [InlineData("2.5", 0, null, null, "3")]
    [InlineData("-5", 0, "0", null, "0")]            // 小於下限修正為下限
    [InlineData("15000", 0, null, "9999", "9999")]   // 大於上限修正為上限
    [InlineData("12.344", 2, "0", "100", "12.34")]
    public void Normalize_四捨五入並限制範圍(string value, int decimals, string? min, string? max, string expected)
    {
        static decimal? D(string? s) => s is null ? null : decimal.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(D(expected), BaseNumberFormat.Normalize(D(value), decimals, D(min), D(max)));
    }

    [Fact]
    public void ToValue_各數值型別轉換()
    {
        Assert.Equal(12, BaseNumberFormat.ToValue<int>(12m));
        Assert.Equal(0, BaseNumberFormat.ToValue<int>(null));          // 不可為 null 的型別遇 null 為 0
        Assert.Null(BaseNumberFormat.ToValue<int?>(null));
        Assert.Equal(12, BaseNumberFormat.ToValue<int?>(12m));
        Assert.Equal(1.5m, BaseNumberFormat.ToValue<decimal?>(1.5m));
        Assert.Equal(2.25d, BaseNumberFormat.ToValue<double>(2.25m));
        Assert.Equal(9999999999L, BaseNumberFormat.ToValue<long>(9999999999m));
    }

    [Fact]
    public void FromValue_轉成decimal()
    {
        Assert.Equal(12m, BaseNumberFormat.FromValue(12));
        Assert.Null(BaseNumberFormat.FromValue<int?>(null));
        Assert.Equal(2.5m, BaseNumberFormat.FromValue(2.5d));
    }

    [Fact]
    public void IsIntegerType_判斷整數型別()
    {
        Assert.True(BaseNumberFormat.IsIntegerType<int>());
        Assert.True(BaseNumberFormat.IsIntegerType<long?>());
        Assert.False(BaseNumberFormat.IsIntegerType<decimal>());
        Assert.False(BaseNumberFormat.IsIntegerType<double?>());
    }
}

/// <summary>
/// 按鈕快速鍵 (依文字自動帶入)
/// </summary>
public class BaseAccessKeysTests
{
    [Theory]
    [InlineData("查詢", "Y")]
    [InlineData("新增", "A")]
    [InlineData("重設", "C")]
    [InlineData("返回", "X")]
    [InlineData("儲存", "Y")]
    [InlineData("確定", "Y")]
    [InlineData("取消", "N")]
    [InlineData("匯出", "O")]
    [InlineData("匯入", "I")]
    [InlineData("列印", "R")]
    [InlineData("報表列印", "R")]
    [InlineData("資料列印", "S")]
    public void Resolve_依文字自動帶入(string text, string expected)
    {
        Assert.Equal(expected, BaseAccessKeys.Resolve(text, null));
    }

    [Theory]
    [InlineData("報表列印（明細）", "R")]
    [InlineData("資料列印說明", "S")]   // 較長的「資料列印」優先於「列印」
    [InlineData("查詢明細", "Y")]
    public void Resolve_以文字開頭比對(string text, string expected)
    {
        Assert.Equal(expected, BaseAccessKeys.Resolve(text, null));
    }

    [Fact]
    public void Resolve_未定義的文字不使用快速鍵()
    {
        Assert.Null(BaseAccessKeys.Resolve("編輯", null));
        Assert.Null(BaseAccessKeys.Resolve("處理中示範", null));
    }

    [Fact]
    public void Resolve_指定快速鍵優先並轉大寫()
    {
        Assert.Equal("P", BaseAccessKeys.Resolve("查詢", "p"));
    }

    [Fact]
    public void Resolve_空字串表示不使用()
    {
        Assert.Null(BaseAccessKeys.Resolve("查詢", ""));
    }
}

/// <summary>
/// 起訖日期規則
/// </summary>
public class BaseDateRangeRulesTests
{
    private static readonly DateTime Oct12 = new(2026, 10, 12);

    [Fact]
    public void 任一日期未輸入時不檢查()
    {
        Assert.Null(BaseDateRangeRules.GetError(null, Oct12, 3));
        Assert.Null(BaseDateRangeRules.GetError(Oct12, null, 3));
    }

    [Fact]
    public void 起日大於迄日()
    {
        Assert.Equal("起日不可大於迄日", BaseDateRangeRules.GetError(Oct12, Oct12.AddDays(-1), 0));
    }

    [Fact]
    public void 同一天正確()
    {
        Assert.Null(BaseDateRangeRules.GetError(Oct12, Oct12, 3));
    }

    [Fact]
    public void 區間剛好3個月正確_多1天錯誤()
    {
        Assert.Null(BaseDateRangeRules.GetError(Oct12, new DateTime(2027, 1, 12), 3));
        Assert.Equal("查詢區間最長為 3 個月，請重新設定", BaseDateRangeRules.GetError(Oct12, new DateTime(2027, 1, 13), 3));
    }

    [Fact]
    public void 忽略時間只比較日期()
    {
        Assert.Null(BaseDateRangeRules.GetError(Oct12.AddHours(23), Oct12.AddHours(1), 0));
    }

    [Fact]
    public void 月數0表示不限制()
    {
        Assert.Null(BaseDateRangeRules.GetError(Oct12, Oct12.AddYears(5), 0));
    }
}

/// <summary>
/// 民國年日期
/// </summary>
public class BaseDateFormatTests
{
    [Theory]
    [InlineData(2026, 10, 12, "115/10/12")]
    [InlineData(2012, 1, 1, "101/01/01")]
    [InlineData(1912, 12, 31, "001/12/31")]
    public void ToRoc_西元轉民國(int year, int month, int day, string expected)
    {
        Assert.Equal(expected, BaseDateFormat.ToRoc(new DateTime(year, month, day)));
    }

    [Fact]
    public void ToRoc_null回傳空字串()
    {
        Assert.Equal(string.Empty, BaseDateFormat.ToRoc(null));
    }

    [Fact]
    public void TryParseRoc_民國轉西元()
    {
        Assert.True(BaseDateFormat.TryParseRoc("115/10/12", out var date));
        Assert.Equal(new DateTime(2026, 10, 12), date);
    }

    [Theory]
    [InlineData("99/01/01", 2010, 1, 1)]    // 民國年 2 位
    [InlineData("115/2/5", 2026, 2, 5)]     // 月、日 1 位
    [InlineData(" 115/10/12 ", 2026, 10, 12)]
    public void TryParseRoc_各種寫法(string text, int year, int month, int day)
    {
        Assert.True(BaseDateFormat.TryParseRoc(text, out var date));
        Assert.Equal(new DateTime(year, month, day), date);
    }

    [Theory]
    [InlineData("115/13/01")]   // 月份錯誤
    [InlineData("115/02/30")]   // 日期不存在
    [InlineData("2026/10/12")]  // 西元格式：不可被當成民國 2026 年
    [InlineData("0/01/01")]     // 民國 0 年不存在
    [InlineData("")]
    [InlineData(null)]
    public void TryParseRoc_格式錯誤(string? text)
    {
        Assert.False(BaseDateFormat.TryParseRoc(text, out _));
    }

    [Fact]
    public void FixAdYearTypedAsRoc_民國欄位誤輸入西元年時修正()
    {
        // 在民國年欄位輸入 2026/10/12 會被解析成民國 2026 年 = 西元 3937 年
        var misread = new DateTime(3937, 10, 12);
        Assert.Equal(new DateTime(2026, 10, 12), BaseDateFormat.FixAdYearTypedAsRoc(misread));
    }

    [Fact]
    public void FixAdYearTypedAsRoc_正常日期不變()
    {
        var normal = new DateTime(2026, 10, 12);
        Assert.Equal(normal, BaseDateFormat.FixAdYearTypedAsRoc(normal));
        Assert.Null(BaseDateFormat.FixAdYearTypedAsRoc(null));
    }
}

/// <summary>
/// 電話號碼拆解與組合
/// </summary>
public class BasePhoneFormatTests
{
    [Fact]
    public void 手機拆解與組合()
    {
        Assert.Equal(("0912", "345678"), BasePhoneFormat.SplitMobile("0912-345678"));
        Assert.Equal("0912-345678", BasePhoneFormat.JoinMobile("0912", "345678"));
    }

    [Fact]
    public void 手機只輸入前段()
    {
        Assert.Equal("0912-", BasePhoneFormat.JoinMobile("0912", null));
        Assert.Equal(("0912", null), BasePhoneFormat.SplitMobile("0912-"));
    }

    [Fact]
    public void 手機全部空白為null()
    {
        Assert.Null(BasePhoneFormat.JoinMobile(null, ""));
        Assert.Equal((null, null), BasePhoneFormat.SplitMobile(null));
    }

    [Fact]
    public void 市話含分機()
    {
        Assert.Equal(("02", "23456789", "123"), BasePhoneFormat.SplitTelephone("02-23456789#123"));
        Assert.Equal("02-23456789#123", BasePhoneFormat.JoinTelephone("02", "23456789", "123"));
    }

    [Fact]
    public void 市話無分機()
    {
        Assert.Equal(("037", "123456", null), BasePhoneFormat.SplitTelephone("037-123456"));
        Assert.Equal("037-123456", BasePhoneFormat.JoinTelephone("037", "123456", null));
    }

    [Fact]
    public void 市話不顯示分機時忽略分機()
    {
        Assert.Equal("02-23456789", BasePhoneFormat.JoinTelephone("02", "23456789", "123", includeExtension: false));
    }

    [Fact]
    public void 市話全部空白為null()
    {
        Assert.Null(BasePhoneFormat.JoinTelephone(null, null, null));
    }
}

/// <summary>
/// 上傳檔案檢查
/// </summary>
public class BaseUploadRulesTests
{
    [Theory]
    [InlineData("data.csv")]
    [InlineData("DATA.CSV")]     // 副檔名不分大小寫
    [InlineData("報表.xlsx")]
    public void 允許的副檔名(string fileName)
    {
        Assert.Null(BaseUploadRules.Check(fileName, 100, ".csv, .xlsx", 10));
    }

    [Fact]
    public void 不允許的副檔名()
    {
        Assert.Equal("note.txt：檔案格式不符，僅允許 .csv", BaseUploadRules.Check("note.txt", 100, ".csv", 10));
    }

    [Fact]
    public void 未限制副檔名()
    {
        Assert.Null(BaseUploadRules.Check("note.txt", 100, null, 10));
    }

    [Fact]
    public void 檔案大小剛好上限可上傳_超過1byte不可()
    {
        var limit = 2L * 1024 * 1024;
        Assert.Null(BaseUploadRules.Check("a.csv", limit, ".csv", 2));
        Assert.Equal("a.csv：檔案過大 (2.0 MB)，上限 2 MB", BaseUploadRules.Check("a.csv", limit + 1, ".csv", 2));
    }

    [Theory]
    [InlineData(500, "500 B")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(3 * 1024 * 1024, "3.0 MB")]
    public void FormatSize_顯示檔案大小(long bytes, string expected)
    {
        Assert.Equal(expected, BaseUploadRules.FormatSize(bytes));
    }
}

/// <summary>
/// 手機、市話格式驗證屬性
/// </summary>
public class BaseValidationAttributeTests
{
    [Theory]
    [InlineData("0912-345678", true)]
    [InlineData(null, true)]           // 空值通過 (必填另加 [Required])
    [InlineData("0912345678", false)]  // 缺少 -
    [InlineData("0812-345678", false)] // 不是 09 開頭
    [InlineData("0912-34567", false)]  // 後段不足 6 碼
    [InlineData("0912-", false)]
    public void BaseMobile(string? value, bool expected)
    {
        Assert.Equal(expected, new BaseMobileAttribute().IsValid(value));
    }

    [Theory]
    [InlineData("02-23456789", true)]
    [InlineData("02-23456789#123", true)]
    [InlineData("037-123456", true)]
    [InlineData("0826-12345", true)]
    [InlineData(null, true)]
    [InlineData("2-23456789", false)]        // 區碼需 0 開頭
    [InlineData("02-1234", false)]           // 號碼不足 5 碼
    [InlineData("02-23456789#", false)]      // 有 # 但無分機
    [InlineData("02-23456789#1234567", false)] // 分機超過 6 碼
    [InlineData("02-", false)]
    public void BaseTelephone(string? value, bool expected)
    {
        Assert.Equal(expected, new BaseTelephoneAttribute().IsValid(value));
    }

    [Fact]
    public void 錯誤訊息為中文()
    {
        var context = new ValidationContext(new object()) { DisplayName = "手機" };
        var result = new BaseMobileAttribute().GetValidationResult("123", context);
        Assert.Equal("手機號碼格式應為 09XX-XXXXXX", result?.ErrorMessage);
    }
}
