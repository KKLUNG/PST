using System.Globalization;
using BlazorUserManagement.Data.Cms;
using BlazorUserManagement.Models;
using BlazorUserManagement.Services;

namespace BlazorUserManagement.Tests;

/// <summary>
/// 測試用的時鐘 (固定時間，可前進)
/// </summary>
public sealed class TestClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 10, 13, 9, 30, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => Now;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

public enum TestColor { Red, Green }

/// <summary>
/// 涵蓋各種型別的測試模型
/// </summary>
public class CmsTestModel
{
    [CmsColumn("SX01")] public string? Text { get; set; }
    [CmsColumn("SX02")] public int Int { get; set; }
    [CmsColumn("SX03")] public int? NullableInt { get; set; }
    [CmsColumn("SX04")] public decimal? Amount { get; set; }
    [CmsColumn("SX05")] public bool Flag { get; set; }
    [CmsColumn("SX06")] public bool? NullableFlag { get; set; }
    [CmsColumn("SX07")] public long Long { get; set; }
    [CmsColumn("SX08")] public double Double { get; set; }
    [CmsColumn("SX09")] public TestColor Color { get; set; }
    [CmsColumn("SX10")] public Guid? Id { get; set; }
    [CmsColumn("SX11")] public DateTime? TextDateTime { get; set; }
    [CmsColumn("SH01")] public string? LongText { get; set; }
    [CmsColumn("DE01")] public DateTime? DateTimeValue { get; set; }
    [CmsColumn("DA01")] public DateTime? DateValue { get; set; }
    [CmsColumn("DA08")] public DateTime RequiredDate { get; set; }
    [CmsColumn("SX64")] public string? LastColumn { get; set; }

    /// <summary>未標註，不存放</summary>
    public string? NotStored { get; set; }
}

/// <summary>
/// CMSData 欄位對應與轉換
/// </summary>
public class CmsMapperTests
{
    private static CmsTestModel Sample() => new()
    {
        Text = "中文測試",
        Int = 42,
        NullableInt = 7,
        Amount = 1234567.89m,
        Flag = true,
        NullableFlag = false,
        Long = 9_876_543_210L,
        Double = 3.25,
        Color = TestColor.Green,
        Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
        TextDateTime = new DateTime(2026, 10, 13, 14, 5, 6),
        LongText = new string('長', 10_000),
        DateTimeValue = new DateTime(2026, 10, 13, 14, 5, 6),
        DateValue = new DateTime(2026, 10, 13, 23, 59, 59),
        RequiredDate = new DateTime(2026, 1, 1),
        LastColumn = "最後一欄",
        NotStored = "不會存",
    };

    [Fact]
    public void 存入再讀出_各型別值不變()
    {
        var entity = new CmsData();
        CmsMapper.Apply(Sample(), entity);

        var model = CmsMapper.ToModel<CmsTestModel>(entity);

        var expected = Sample();
        Assert.Equal(expected.Text, model.Text);
        Assert.Equal(expected.Int, model.Int);
        Assert.Equal(expected.NullableInt, model.NullableInt);
        Assert.Equal(expected.Amount, model.Amount);
        Assert.Equal(expected.Flag, model.Flag);
        Assert.Equal(expected.NullableFlag, model.NullableFlag);
        Assert.Equal(expected.Long, model.Long);
        Assert.Equal(expected.Double, model.Double);
        Assert.Equal(expected.Color, model.Color);
        Assert.Equal(expected.Id, model.Id);
        Assert.Equal(expected.TextDateTime, model.TextDateTime);
        Assert.Equal(expected.LongText, model.LongText);
        Assert.Equal(expected.DateTimeValue, model.DateTimeValue);
        Assert.Equal(expected.RequiredDate, model.RequiredDate);
        Assert.Equal(expected.LastColumn, model.LastColumn);
        Assert.Null(model.NotStored);
    }

    [Fact]
    public void 存放的文字格式固定()
    {
        var entity = new CmsData();
        CmsMapper.Apply(Sample(), entity);

        Assert.Equal("42", entity.SX02);
        Assert.Equal("1234567.89", entity.SX04);           // 不含千分位、小數點固定為「.」
        Assert.Equal("Y", entity.SX05);                    // 是否以 Y / N 存放
        Assert.Equal("N", entity.SX06);
        Assert.Equal("Green", entity.SX09);                // 列舉以名稱存放
        Assert.Equal("2026-10-13 14:05:06", entity.SX11);  // 日期時間存在文字欄位的格式
        Assert.Equal("最後一欄", entity.SX64);
    }

    [Fact]
    public void 日期欄位DA去除時間_DE保留時間()
    {
        var entity = new CmsData();
        CmsMapper.Apply(Sample(), entity);

        Assert.Equal(new DateTime(2026, 10, 13), entity.DA01);
        Assert.Equal(new DateTime(2026, 10, 13, 14, 5, 6), entity.DE01);
    }

    [Fact]
    public void 文字格式不受系統語系影響()
    {
        // 德文語系的小數點是逗號 (1234567,89)；存放格式仍需為 1234567.89
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var entity = new CmsData();
            CmsMapper.Apply(Sample(), entity);

            Assert.Equal("1234567.89", entity.SX04);
            Assert.Equal(1234567.89m, CmsMapper.ToModel<CmsTestModel>(entity).Amount);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Null值存成null_讀回時可為null型別為null_不可為null型別為預設值()
    {
        var entity = new CmsData();
        CmsMapper.Apply(new CmsTestModel(), entity);

        Assert.Null(entity.SX01);
        Assert.Null(entity.SX03);
        Assert.Null(entity.DA01);

        var model = CmsMapper.ToModel<CmsTestModel>(new CmsData());
        Assert.Null(model.NullableInt);
        Assert.Equal(0, model.Int);
        Assert.False(model.Flag);
        Assert.Equal(default, model.RequiredDate);
    }

    [Fact]
    public void 只寫入有對應的欄位_其他欄位保持原值()
    {
        var entity = new CmsData { SX30 = "其他畫面的資料", PageName = "X" };

        CmsMapper.Apply(Sample(), entity);

        Assert.Equal("其他畫面的資料", entity.SX30);
        Assert.Equal("X", entity.PageName);
    }

    [Theory]
    [InlineData("1.250", "1.25")]
    [InlineData("100.00", "100")]
    [InlineData("0.0", "0")]
    [InlineData("-12.50", "-12.5")]
    public void 小數去除尾端0_相同數值存成相同文字(string input, string expected)
    {
        // 曾發生的問題：輸入 1.250 存成 "1.250"，以 '1.25' 查詢不到
        var value = decimal.Parse(input, CultureInfo.InvariantCulture);
        Assert.Equal(expected, CmsMapper.ToText(value));
    }

    [Theory]
    [InlineData("Y", true)]
    [InlineData("n", false)]
    [InlineData("true", true)]
    [InlineData("0", false)]
    public void 是否值可接受多種寫法(string text, bool expected)
    {
        Assert.Equal(expected, CmsMapper.FromText(text, typeof(bool)));
    }

    [Fact]
    public void 資料內容無法轉換時說明是哪個欄位()
    {
        var entity = new CmsData { SX02 = "abc" };

        var ex = Assert.Throws<FormatException>(() => CmsMapper.ToModel<CmsTestModel>(entity));

        Assert.Contains("SX02", ex.Message);
        Assert.Contains("Int", ex.Message);
        Assert.Contains("abc", ex.Message);
    }

    // ---------- 對應設定錯誤 ----------

    private class UnknownColumnModel
    {
        [CmsColumn("SX65")] public string? A { get; set; }
    }

    private class DuplicateColumnModel
    {
        [CmsColumn("SX01")] public string? A { get; set; }
        [CmsColumn("SX01")] public string? B { get; set; }
    }

    private class DateColumnWrongTypeModel
    {
        [CmsColumn("DE01")] public string? A { get; set; }
    }

    private class UnsupportedTypeModel
    {
        [CmsColumn("SX01")] public List<int>? A { get; set; }
    }

    [Fact]
    public void 欄位名稱不存在()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => CmsMapper.GetMap<UnknownColumnModel>());
        Assert.Contains("沒有欄位 SX65", ex.Message);
    }

    [Fact]
    public void 欄位重複對應()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => CmsMapper.GetMap<DuplicateColumnModel>());
        Assert.Contains("SX01 已被 A 使用", ex.Message);
    }

    [Fact]
    public void 日期欄位對應到非日期屬性()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => CmsMapper.GetMap<DateColumnWrongTypeModel>());
        Assert.Contains("DE01 為日期欄位", ex.Message);
    }

    [Fact]
    public void 不支援的型別()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => CmsMapper.GetMap<UnsupportedTypeModel>());
        Assert.Contains("無法存放在文字欄位 SX01", ex.Message);
    }
}

/// <summary>
/// 各畫面表單的 CMSData 對應：設定正確、存入讀出不變
/// (新增欄位時若對應到已使用的欄位，這裡會失敗)
/// </summary>
public class CmsFormMappingTests
{
    [Fact]
    public void 寄件登記表單對應設定正確()
    {
        var map = CmsMapper.GetMap<ShipmentForm>();

        Assert.Equal(25, map.Count);   // SX01-SX21、DA01-DA03、SH01
        Assert.Contains(map, f => f.Property.Name == nameof(ShipmentForm.ShipDate) && f.ColumnName == "DA01");
        Assert.Contains(map, f => f.Property.Name == nameof(ShipmentForm.Remark) && f.ColumnName == "SH01");
    }

    [Fact]
    public void 寄件登記表單存入讀出不變()
    {
        var form = new ShipmentForm
        {
            SenderName = "王小明", SenderMobile = "0912-345678", SenderTel = "02-23456789#123",
            SenderCounty = "臺北市", SenderDistrict = "大安區", SenderZipCode = "106", SenderAddress = "忠孝東路一段1號",
            ReceiverName = "李大華", ReceiverMobile = "0922-111222",
            ReceiverCounty = "新北市", ReceiverDistrict = "板橋區", ReceiverZipCode = "220", ReceiverAddress = "文化路一段100號",
            MailType = "2", Quantity = 3, WeightKg = 1.255m, InsuredAmount = 12345m,
            ShipDate = new DateTime(2026, 10, 13), DeliveryStart = new DateTime(2026, 10, 15), DeliveryEnd = new DateTime(2026, 10, 20),
            SendMethod = "Pickup", PayType = "1", NeedSignature = false, Remark = "易碎品\n請小心",
            Source = ShipmentForm.SourceImport,
        };
        var entity = new CmsData();

        CmsMapper.Apply(form, entity);
        var loaded = CmsMapper.ToModel<ShipmentForm>(entity);

        foreach (var field in CmsMapper.GetMap<ShipmentForm>())
        {
            Assert.Equal(field.Property.GetValue(form), field.Property.GetValue(loaded));
        }
    }

    [Fact]
    public void 匯入列轉成寄件表單()
    {
        var row = new ShipmentCsvParser.Row(2, new DateTime(2026, 12, 1), "王小明", "0912-345678", "臺北市大安區", 2, 1.5m);

        var form = ShipmentForm.FromImport(row);

        Assert.Equal(ShipmentForm.SourceImport, form.Source);
        Assert.Equal("1", form.MailType);
        Assert.Equal("王小明", form.ReceiverName);
        Assert.Equal(1.5m, form.WeightKg);
    }

    [Fact]
    public void 元件展示表單對應設定正確_密碼不儲存()
    {
        var map = CmsMapper.GetMap<ComponentDemoForm>();

        Assert.Equal(23, map.Count);   // SX01-SX18、DA01-DA04、SH01
        Assert.DoesNotContain(map, f => f.Property.Name == nameof(ComponentDemoForm.Password));
    }

    [Fact]
    public void 元件展示表單存入讀出不變()
    {
        var form = new ComponentDemoForm
        {
            UserId = "demo", BranchNo = "123456", ProductCode = "AB12", Unit = "B", Status = false,
            Qty = 12, Amount = 1234567.89m, AdDate = new DateTime(2026, 10, 12), RocDate = new DateTime(2026, 10, 13),
            Gender = "F", Agree = true, PayType = "2", ParamUnit = "A",
            StartDate = new DateTime(2026, 10, 1), EndDate = new DateTime(2026, 12, 31),
            Mobile = "0912-345678", Tel = "02-23456789", County = "臺北市", District = "大安區", ZipCode = "106",
            AddressDetail = "忠孝東路一段1號", FullAddress = "臺北市大安區忠孝東路一段1號", Remark = "備註",
            Password = "secret",
        };
        var entity = new CmsData();

        CmsMapper.Apply(form, entity);
        var loaded = CmsMapper.ToModel<ComponentDemoForm>(entity);

        foreach (var field in CmsMapper.GetMap<ComponentDemoForm>())
        {
            Assert.Equal(field.Property.GetValue(form), field.Property.GetValue(loaded));
        }
        Assert.Null(loaded.Password);
    }
}

/// <summary>
/// CMSData 存取服務
/// </summary>
public class CmsDataServiceTests
{
    private readonly TestDbContextFactory factory = new();
    private readonly TestClock clock = new();
    private readonly CmsDataService service;

    public CmsDataServiceTests()
    {
        service = new CmsDataService(factory, clock);
    }

    [Fact]
    public async Task 新增_產生PKey並記錄建立時間()
    {
        var pkey = await service.SaveAsync("PageA", new CmsTestModel { Text = "新增", Int = 1 });

        var record = await service.GetAsync<CmsTestModel>(pkey);

        Assert.NotEqual(Guid.Empty, pkey);
        Assert.NotNull(record);
        Assert.Equal("新增", record!.Model.Text);
        Assert.Equal(new DateTime(2026, 10, 13, 9, 30, 0), record.CreateDate);
        Assert.Null(record.ModifiedDate);
    }

    [Fact]
    public async Task 修改_更新內容與修改時間_建立時間不變()
    {
        var pkey = await service.SaveAsync("PageA", new CmsTestModel { Text = "原本" });
        clock.Now = clock.Now.AddHours(2);

        var returned = await service.SaveAsync("PageA", new CmsTestModel { Text = "修改後" }, pkey);
        var record = await service.GetAsync<CmsTestModel>(pkey);

        Assert.Equal(pkey, returned);
        Assert.Equal("修改後", record!.Model.Text);
        Assert.Equal(new DateTime(2026, 10, 13, 9, 30, 0), record.CreateDate);
        Assert.Equal(new DateTime(2026, 10, 13, 11, 30, 0), record.ModifiedDate);
    }

    [Fact]
    public async Task 不可修改其他畫面的資料()
    {
        var pkey = await service.SaveAsync("PageA", new CmsTestModel());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync("PageB", new CmsTestModel(), pkey));
        Assert.Contains("不屬於畫面 PageB", ex.Message);
    }

    [Fact]
    public async Task 修改不存在的資料()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync("PageA", new CmsTestModel(), Guid.NewGuid()));
    }

    [Fact]
    public async Task 清單只包含該畫面_依建立時間排序()
    {
        await service.SaveAsync("PageA", new CmsTestModel { Text = "A1" });
        clock.Now = clock.Now.AddMinutes(1);
        await service.SaveAsync("PageB", new CmsTestModel { Text = "B1" });
        clock.Now = clock.Now.AddMinutes(1);
        await service.SaveAsync("PageA", new CmsTestModel { Text = "A2" });

        var list = await service.ListAsync<CmsTestModel>("PageA");

        Assert.Equal(["A1", "A2"], list.Select(r => r.Model.Text));
    }

    [Fact]
    public async Task 一次新增多筆()
    {
        var count = await service.AddRangeAsync("Import", [new CmsTestModel { Int = 1 }, new CmsTestModel { Int = 2 }, new CmsTestModel { Int = 3 }]);

        Assert.Equal(3, count);
        Assert.Equal([1, 2, 3], (await service.ListAsync<CmsTestModel>("Import")).Select(r => r.Model.Int).Order());
    }

    [Fact]
    public async Task 刪除()
    {
        var pkey = await service.SaveAsync("PageA", new CmsTestModel());

        Assert.True(await service.DeleteAsync(pkey));
        Assert.Null(await service.GetAsync<CmsTestModel>(pkey));
        Assert.False(await service.DeleteAsync(pkey));
    }

    [Fact]
    public async Task 寄件表單整筆存入資料庫再讀出()
    {
        var form = new ShipmentForm
        {
            SenderName = "王小明", SenderMobile = "0912-345678", ReceiverName = "李大華", ReceiverMobile = "0922-111222",
            ReceiverAddress = "文化路一段100號", MailType = "1", Quantity = 2, WeightKg = 1.25m, InsuredAmount = 12345m,
            ShipDate = new DateTime(2026, 10, 13), PayType = "1",
        };

        var pkey = await service.SaveAsync(ShipmentForm.CmsPageName, form);
        var loaded = (await service.GetAsync<ShipmentForm>(pkey))!.Model;

        Assert.Equal("王小明", loaded.SenderName);
        Assert.Equal(12345m, loaded.InsuredAmount);
        Assert.Equal(1.25m, loaded.WeightKg);
        Assert.Equal(new DateTime(2026, 10, 13), loaded.ShipDate);
        Assert.True(loaded.NeedSignature);
        Assert.Equal(ShipmentForm.SourceScreen, loaded.Source);
    }
}
