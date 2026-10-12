using System.Text;
using BlazorUserManagement.Models;
using BlazorUserManagement.Services;

namespace BlazorUserManagement.Tests;

/// <summary>
/// 寄件登記表單驗證
/// </summary>
public class ShipmentFormTests
{
    /// <summary>一份所有欄位都正確的表單</summary>
    private static ShipmentForm ValidForm() => new()
    {
        SenderName = "王小明",
        SenderMobile = "0912-345678",
        SenderCounty = "臺北市",
        SenderDistrict = "大安區",
        SenderZipCode = "106",
        SenderAddress = "忠孝東路一段1號",
        ReceiverName = "李大華",
        ReceiverMobile = "0922-111222",
        ReceiverCounty = "新北市",
        ReceiverDistrict = "板橋區",
        ReceiverZipCode = "220",
        ReceiverAddress = "文化路一段100號",
        MailType = "1",
        Quantity = 1,
        WeightKg = 1.25m,
        ShipDate = DateTime.Today,
        PayType = "1",
    };

    [Fact]
    public void 正確的表單驗證通過()
    {
        Assert.Empty(ValidationHelper.Validate(ValidForm()));
    }

    [Fact]
    public void 寄件人手機電話都沒填()
    {
        var form = ValidForm();
        form.SenderMobile = null;
        form.SenderTel = null;

        Assert.Contains((nameof(ShipmentForm.SenderMobile), "寄件人手機、電話至少填寫一項"), ValidationHelper.Validate(form));
    }

    [Fact]
    public void 寄件人只填電話也可以()
    {
        var form = ValidForm();
        form.SenderMobile = null;
        form.SenderTel = "02-23456789";

        Assert.Empty(ValidationHelper.Validate(form));
    }

    [Fact]
    public void 到府收件必須填寄件人手機()
    {
        var form = ValidForm();
        form.SendMethod = "Pickup";
        form.SenderMobile = null;
        form.SenderTel = "02-23456789";

        Assert.Contains((nameof(ShipmentForm.SenderMobile), "到府收件須填寫寄件人手機"), ValidationHelper.Validate(form));
    }

    [Fact]
    public void 寄件日期不可早於今天()
    {
        var form = ValidForm();
        form.ShipDate = DateTime.Today.AddDays(-1);

        Assert.Contains((nameof(ShipmentForm.ShipDate), "寄件日期不可早於今天"), ValidationHelper.Validate(form));
    }

    [Fact]
    public void 指定送達期間不可早於寄件日期()
    {
        var form = ValidForm();
        form.ShipDate = DateTime.Today.AddDays(3);
        form.DeliveryStart = DateTime.Today.AddDays(1);
        form.DeliveryEnd = DateTime.Today.AddDays(5);

        Assert.Contains((nameof(ShipmentForm.DeliveryEnd), "指定送達期間不可早於寄件日期"), ValidationHelper.Validate(form));
    }

    [Fact]
    public void 地址未選鄉鎮市區()
    {
        var form = ValidForm();
        form.ReceiverDistrict = null;

        Assert.Contains((nameof(ShipmentForm.ReceiverAddress), "請選擇收件人地址的縣市與鄉鎮市區"), ValidationHelper.Validate(form));
    }

    [Theory]
    [InlineData("0.001", true)]
    [InlineData("30", true)]
    [InlineData("0", false)]
    [InlineData("30.001", false)]
    public void 重量範圍(string weight, bool valid)
    {
        var form = ValidForm();
        form.WeightKg = decimal.Parse(weight, System.Globalization.CultureInfo.InvariantCulture);

        var errors = ValidationHelper.Validate(form);
        Assert.Equal(valid, !errors.Any(e => e.Member == nameof(ShipmentForm.WeightKg)));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(99, true)]
    [InlineData(0, false)]
    [InlineData(100, false)]
    public void 件數範圍(int quantity, bool valid)
    {
        var form = ValidForm();
        form.Quantity = quantity;

        var errors = ValidationHelper.Validate(form);
        Assert.Equal(valid, !errors.Any(e => e.Member == nameof(ShipmentForm.Quantity)));
    }

    [Fact]
    public void 保價金額上限()
    {
        var form = ValidForm();
        form.InsuredAmount = 1_000_001m;

        Assert.Contains(ValidationHelper.Validate(form), e => e.Member == nameof(ShipmentForm.InsuredAmount));
    }

    [Fact]
    public void 收件人手機格式錯誤()
    {
        var form = ValidForm();
        form.ReceiverMobile = "0922111222";

        Assert.Contains((nameof(ShipmentForm.ReceiverMobile), "手機號碼格式應為 09XX-XXXXXX"), ValidationHelper.Validate(form));
    }

    [Fact]
    public void 必填欄位未填()
    {
        var errors = ValidationHelper.Validate(new ShipmentForm());

        Assert.Contains(errors, e => e.Message == "請輸入寄件人姓名");
        Assert.Contains(errors, e => e.Message == "請輸入收件人手機");
        Assert.Contains(errors, e => e.Message == "請選擇郵件種類");
        Assert.Contains(errors, e => e.Message == "請選擇付款方式");
        Assert.Contains(errors, e => e.Message == "請選擇寄件日期");
    }
}

/// <summary>
/// 寄件批次匯入 CSV 解析
/// </summary>
public class ShipmentCsvParserTests
{
    private static readonly DateTime Today = new(2026, 10, 12);
    private const string Header = "寄件日期(民國),收件人姓名,收件人手機,收件人地址,件數,重量(公斤)";

    private static byte[] Csv(params string[] lines) => Encoding.UTF8.GetBytes(string.Join("\r\n", lines));

    [Fact]
    public void 正確的檔案()
    {
        var (rows, errors) = ShipmentCsvParser.Parse(Csv(Header,
            "115/10/12,王小明,0912-345678,臺北市大安區忠孝東路一段1號,1,1.25",
            "115/10/20,李大華,0922-111222,新北市板橋區文化路一段100號,2,3.5"), Today);

        Assert.Empty(errors);
        Assert.Equal(2, rows.Count);
        Assert.Equal(new DateTime(2026, 10, 12), rows[0].ShipDate);
        Assert.Equal(1.25m, rows[0].WeightKg);
        Assert.Equal(2, rows[1].Quantity);
    }

    [Fact]
    public void 含BOM與空白列()
    {
        var content = Encoding.UTF8.GetPreamble()
            .Concat(Csv(Header, "", "115/10/12,王小明,0912-345678,臺北市,1,1", ""))
            .ToArray();

        var (rows, errors) = ShipmentCsvParser.Parse(content, Today);

        Assert.Empty(errors);
        Assert.Single(rows);
    }

    [Fact]
    public void 只有標題列()
    {
        var (rows, errors) = ShipmentCsvParser.Parse(Csv(Header), Today);

        Assert.Empty(rows);
        Assert.Equal(["檔案沒有資料列"], errors);
    }

    [Fact]
    public void 每列錯誤標示列號與原因()
    {
        var (rows, errors) = ShipmentCsvParser.Parse(Csv(Header,
            "115/10/12,王小明,0912-345678,臺北市,1,1",          // 第 2 列正確
            "115/10/11,李大華,0922-111222,新北市,1,1",          // 第 3 列：日期早於今天
            "2026/10/12,張三,0912345678,臺中市,0,31",          // 第 4 列：多項錯誤
            "115/10/12,王五"), Today);                          // 第 5 列：欄位不足

        Assert.Single(rows);
        Assert.Equal(3, errors.Count);
        Assert.Equal("第 3 列：寄件日期不可早於今天", errors[0]);
        Assert.Equal("第 4 列：寄件日期格式應為民國年 yyy/MM/dd、收件人手機格式應為 09XX-XXXXXX、件數須為 1-99 的整數、重量須介於 0.001-30 公斤", errors[1]);
        Assert.Equal("第 5 列：欄位數不足，應為 6 欄", errors[2]);
    }

    [Fact]
    public void 範本檔可以正確匯入()
    {
        // 範本檔的日期 (116/12/01) 需晚於測試日；範本檔由專案設定複製到測試輸出目錄
        var content = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "templates", "shipments-sample.csv"));

        var (rows, errors) = ShipmentCsvParser.Parse(content, Today);

        Assert.Empty(errors);
        Assert.Equal(2, rows.Count);
    }
}
