using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

namespace BaseComponents.Tests;

/// <summary>
/// 元件測試共用設定：註冊 Fluent UI 服務，JavaScript 呼叫一律回傳預設值
/// </summary>
public abstract class ComponentTestBase : BunitContext
{
    protected ComponentTestBase()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddFluentUIComponents();
    }
}

public class BaseButtonTests : ComponentTestBase
{
    [Fact]
    public void 顯示文字與自動快速鍵()
    {
        var cut = Render<BaseButton>(p => p.Add(x => x.Text, "查詢"));

        var button = cut.Find("fluent-button");
        Assert.Equal("y", button.GetAttribute("accesskey"));
        Assert.Contains("查詢(Y)", button.TextContent.Replace(" ", ""));
    }

    [Fact]
    public void 主要按鈕標記為鍵盤操作的目標()
    {
        var cut = Render<BaseButton>(p => p
            .Add(x => x.Text, "儲存")
            .Add(x => x.Kind, BaseButtonKind.Primary));

        Assert.Equal("true", cut.Find("fluent-button").GetAttribute("data-base-primary"));
    }

    [Fact]
    public void 不使用快速鍵()
    {
        var cut = Render<BaseButton>(p => p.Add(x => x.Text, "查詢").Add(x => x.AccessKey, ""));

        Assert.False(cut.Find("fluent-button").HasAttribute("accesskey"));
        Assert.DoesNotContain("(", cut.Find("fluent-button").TextContent);
    }

    [Fact]
    public void 點擊執行中自動停用_完成後恢復()
    {
        var running = new TaskCompletionSource();
        var clicks = 0;
        var cut = Render<BaseButton>(p => p
            .Add(x => x.Text, "匯出")
            .Add(x => x.OnClick, async () => { clicks++; await running.Task; }));

        cut.Find("fluent-button").Click();
        Assert.True(cut.Find("fluent-button").HasAttribute("disabled"));

        // 執行中再點一次不會重複執行
        cut.Find("fluent-button").Click();
        Assert.Equal(1, clicks);

        running.SetResult();
        cut.WaitForAssertion(() => Assert.False(cut.Find("fluent-button").HasAttribute("disabled")));
    }
}

public class BaseNumericTests : ComponentTestBase
{
    [Fact]
    public void 顯示千分位()
    {
        var cut = Render<BaseNumeric<decimal?>>(p => p
            .Add(x => x.Value, 1234567.5m)
            .Add(x => x.DecimalDigits, 2));

        var field = cut.Find("fluent-text-field");
        Assert.Equal("1,234,567.5", field.GetAttribute("value"));
        Assert.Equal("1234567.5", field.GetAttribute("data-raw"));
    }

    [Fact]
    public void 固定小數位數()
    {
        var cut = Render<BaseNumeric<decimal>>(p => p
            .Add(x => x.Value, 1234m)
            .Add(x => x.FixedDecimals, true));

        Assert.Equal("1,234.00", cut.Find("fluent-text-field").GetAttribute("value"));
    }

    [Fact]
    public void 整數型別預設不顯示小數()
    {
        var cut = Render<BaseNumeric<int>>(p => p.Add(x => x.Value, 9876543));

        var field = cut.Find("fluent-text-field");
        Assert.Equal("9,876,543", field.GetAttribute("value"));
        Assert.Equal("0", field.GetAttribute("data-dec-digits"));
    }

    [Fact]
    public void 輸入後解析並通知新值()
    {
        decimal? received = null;
        var cut = Render<BaseNumeric<decimal?>>(p => p
            .Add(x => x.Value, null)
            .Add(x => x.ValueChanged, v => received = v));

        cut.Find("fluent-text-field").Change("12,345.678");

        Assert.Equal(12345.68m, received);   // 預設小數 2 位，四捨五入
        Assert.Equal("12,345.68", cut.Find("fluent-text-field").GetAttribute("value"));
    }

    [Fact]
    public void 超出範圍修正為上下限()
    {
        int? received = null;
        var cut = Render<BaseNumeric<int?>>(p => p
            .Add(x => x.Min, 1)
            .Add(x => x.Max, 99)
            .Add(x => x.ValueChanged, v => received = v));

        cut.Find("fluent-text-field").Change("150");
        Assert.Equal(99, received);

        cut.Find("fluent-text-field").Change("0");
        Assert.Equal(1, received);
    }

    [Fact]
    public void 清空時可為null型別回傳null()
    {
        int? received = 5;
        var cut = Render<BaseNumeric<int?>>(p => p
            .Add(x => x.Value, 5)
            .Add(x => x.ValueChanged, v => received = v));

        cut.Find("fluent-text-field").Change("");

        Assert.Null(received);
    }

    [Fact]
    public void 最小值大於等於0時不允許負數()
    {
        var cut = Render<BaseNumeric<int>>(p => p.Add(x => x.Min, 0));

        Assert.Equal("false", cut.Find("fluent-text-field").GetAttribute("data-negative"));
    }
}

public class BaseMobileTests : ComponentTestBase
{
    [Fact]
    public void 拆成前後兩段顯示()
    {
        var cut = Render<BaseMobile>(p => p.Add(x => x.Value, "0912-345678"));

        var parts = cut.FindAll("fluent-text-field");
        Assert.Equal("0912", parts[0].GetAttribute("value"));
        Assert.Equal("345678", parts[1].GetAttribute("value"));
    }

    [Fact]
    public void 修改後段組成完整號碼()
    {
        string? received = null;
        var cut = Render<BaseMobile>(p => p
            .Add(x => x.Value, "0912-345678")
            .Add(x => x.ValueChanged, v => received = v));

        cut.FindAll("fluent-text-field")[1].Input("111222");

        Assert.Equal("0912-111222", received);
    }

    [Fact]
    public void 只能輸入數字且滿長度自動跳欄()
    {
        var cut = Render<BaseMobile>();

        foreach (var part in cut.FindAll("fluent-text-field"))
        {
            Assert.Equal("true", part.GetAttribute("data-digits-only"));
            Assert.Equal("true", part.GetAttribute("data-auto-next"));
        }
    }
}

public class BaseTelephoneTests : ComponentTestBase
{
    [Fact]
    public void 拆成區碼號碼分機()
    {
        var cut = Render<BaseTelephone>(p => p.Add(x => x.Value, "02-23456789#123"));

        var parts = cut.FindAll("fluent-text-field");
        Assert.Equal(3, parts.Count);
        Assert.Equal("02", parts[0].GetAttribute("value"));
        Assert.Equal("23456789", parts[1].GetAttribute("value"));
        Assert.Equal("123", parts[2].GetAttribute("value"));
    }

    [Fact]
    public void 不顯示分機()
    {
        var cut = Render<BaseTelephone>(p => p.Add(x => x.ShowExtension, false));

        Assert.Equal(2, cut.FindAll("fluent-text-field").Count);
    }

    [Fact]
    public void 輸入組成完整號碼()
    {
        string? received = null;
        var cut = Render<BaseTelephone>(p => p.Add(x => x.ValueChanged, v => received = v));

        var parts = cut.FindAll("fluent-text-field");
        parts[0].Input("02");
        cut.FindAll("fluent-text-field")[1].Input("23456789");

        Assert.Equal("02-23456789", received);
    }
}

public class BaseDateRangeTests : ComponentTestBase
{
    private class Model
    {
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
    }

    [Fact]
    public async Task 表單中起日大於迄日_驗證失敗並顯示在迄日()
    {
        var model = new Model { Start = new DateTime(2026, 10, 12), End = new DateTime(2026, 10, 1) };
        var editContext = new EditContext(model);

        var cut = Render<CascadingValue<EditContext>>(p => p
            .Add(x => x.Value, editContext)
            .AddChildContent<BaseDateRange>(c => c
                .Add(x => x.StartValue, model.Start)
                .Add(x => x.StartValueExpression, () => model.Start)
                .Add(x => x.EndValue, model.End)
                .Add(x => x.EndValueExpression, () => model.End)));

        var valid = await cut.InvokeAsync(() => editContext.Validate());

        Assert.False(valid);
        Assert.Contains("起日不可大於迄日", editContext.GetValidationMessages(FieldIdentifier.Create(() => model.End)));
    }

    [Fact]
    public async Task 表單中區間正確_驗證通過()
    {
        var model = new Model { Start = new DateTime(2026, 10, 12), End = new DateTime(2026, 12, 31) };
        var editContext = new EditContext(model);

        var cut = Render<CascadingValue<EditContext>>(p => p
            .Add(x => x.Value, editContext)
            .AddChildContent<BaseDateRange>(c => c
                .Add(x => x.StartValue, model.Start)
                .Add(x => x.StartValueExpression, () => model.Start)
                .Add(x => x.EndValue, model.End)
                .Add(x => x.EndValueExpression, () => model.End)
                .Add(x => x.MaxMonths, 3)));

        Assert.True(await cut.InvokeAsync(() => editContext.Validate()));
    }

    [Fact]
    public async Task 查詢區呼叫Validate顯示錯誤()
    {
        var cut = Render<BaseDateRange>(p => p
            .Add(x => x.StartValue, new DateTime(2026, 1, 1))
            .Add(x => x.EndValue, new DateTime(2026, 6, 1))
            .Add(x => x.MaxMonths, 3));

        var valid = await cut.InvokeAsync(() => cut.Instance.Validate());

        Assert.False(valid);
        cut.WaitForAssertion(() => Assert.Contains("查詢區間最長為 3 個月", cut.Markup));
    }
}

public class BaseParameterSelectTests : ComponentTestBase
{
    private class FakeParameterProvider : IBaseParameterProvider
    {
        public Task<IReadOnlyList<BaseOption<string>>> GetOptionsAsync(string setItem) =>
            Task.FromResult<IReadOnlyList<BaseOption<string>>>(setItem == "UNIT"
                ? [new("A", "台北郵局"), new("B", "台中郵局"), new("C", "高雄郵局")]
                : []);
    }

    [Fact]
    public void 選項顯示代碼與名稱()
    {
        Services.AddSingleton<IBaseParameterProvider, FakeParameterProvider>();

        var cut = Render<BaseParameterSelect>(p => p.Add(x => x.SetItem, "UNIT"));

        var options = cut.FindAll("fluent-option").Select(o => o.TextContent.Trim()).Where(t => t != "－ 請選擇 －").ToList();
        Assert.Equal(["A - 台北郵局", "B - 台中郵局", "C - 高雄郵局"], options);
    }

    [Fact]
    public void 排除代碼且不顯示代碼()
    {
        Services.AddSingleton<IBaseParameterProvider, FakeParameterProvider>();

        var cut = Render<BaseParameterSelect>(p => p
            .Add(x => x.SetItem, "UNIT")
            .Add(x => x.ShowCode, false)
            .Add(x => x.Exclude, ["B"]));

        var options = cut.FindAll("fluent-option").Select(o => o.TextContent.Trim()).Where(t => t != "－ 請選擇 －").ToList();
        Assert.Equal(["台北郵局", "高雄郵局"], options);
    }

    [Fact]
    public void 加上全部選項()
    {
        Services.AddSingleton<IBaseParameterProvider, FakeParameterProvider>();

        var cut = Render<BaseParameterSelect>(p => p
            .Add(x => x.SetItem, "UNIT")
            .Add(x => x.AllText, "－ 全部 －"));

        Assert.Equal("－ 全部 －", cut.FindAll("fluent-option")[0].TextContent.Trim());
        Assert.Equal(4, cut.FindAll("fluent-option").Count);
    }

    [Fact]
    public void 未註冊資料來源顯示提示()
    {
        var cut = Render<BaseParameterSelect>(p => p.Add(x => x.SetItem, "UNIT"));

        Assert.Contains("尚未註冊 IBaseParameterProvider", cut.Markup);
    }
}

public class BaseAddressTests : ComponentTestBase
{
    private class FakeAddressProvider : IBaseAddressProvider
    {
        public Task<IReadOnlyList<string>> GetCountiesAsync() =>
            Task.FromResult<IReadOnlyList<string>>(["臺北市", "新北市"]);

        public Task<IReadOnlyList<BaseAddressArea>> GetDistrictsAsync(string county) =>
            Task.FromResult<IReadOnlyList<BaseAddressArea>>(county == "臺北市"
                ? [new("中正區", "100"), new("大安區", "106")]
                : [new("板橋區", "220")]);
    }

    public BaseAddressTests()
    {
        Services.AddSingleton<IBaseAddressProvider, FakeAddressProvider>();
    }

    [Fact]
    public void 載入縣市_未選縣市時鄉鎮市區停用()
    {
        var cut = Render<BaseAddress>();

        var selects = cut.FindAll("fluent-select");
        Assert.Contains("臺北市", selects[0].TextContent);
        Assert.True(selects[1].HasAttribute("disabled"));
    }

    [Fact]
    public void 已有縣市時載入該縣市的鄉鎮市區並顯示郵遞區號()
    {
        var cut = Render<BaseAddress>(p => p
            .Add(x => x.County, "臺北市")
            .Add(x => x.District, "大安區")
            .Add(x => x.ZipCode, "106"));

        var districts = cut.FindAll("fluent-select")[1].TextContent;
        Assert.Contains("大安區", districts);
        Assert.DoesNotContain("板橋區", districts);
        Assert.Contains("106", cut.Find(".base-address-zip").TextContent);
    }

    [Fact]
    public void 組合完整地址()
    {
        Assert.Equal("臺北市大安區忠孝東路一段1號", BaseAddress.Format("臺北市", "大安區", "忠孝東路一段1號"));
        Assert.Equal("忠孝東路", BaseAddress.Format(null, null, "忠孝東路"));
    }
}

public class BaseFileUploadTests : ComponentTestBase
{
    [Fact]
    public void 選擇正確檔案後可匯入並顯示結果()
    {
        IReadOnlyList<BaseUploadFile>? uploaded = null;
        var cut = Render<BaseFileUpload>(p => p
            .Add(x => x.Accept, ".csv")
            .Add(x => x.OnUpload, files =>
            {
                uploaded = files;
                return Task.FromResult(BaseUploadResult.Ok("匯入完成"));
            }));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("a,b\n1,2", "data.csv"));
        cut.WaitForAssertion(() => Assert.Contains("data.csv", cut.Find(".base-upload-files").TextContent));

        cut.Find(".base-upload-actions fluent-button").Click();

        cut.WaitForAssertion(() => Assert.Contains("匯入完成", cut.Find(".base-upload-result.success").TextContent));
        Assert.NotNull(uploaded);
        Assert.Equal("data.csv", uploaded![0].Name);
        Assert.Equal("a,b\n1,2", System.Text.Encoding.UTF8.GetString(uploaded[0].Content));
    }

    [Fact]
    public void 副檔名不符顯示錯誤且不可匯入()
    {
        var called = false;
        var cut = Render<BaseFileUpload>(p => p
            .Add(x => x.Accept, ".csv")
            .Add(x => x.OnUpload, _ => { called = true; return Task.FromResult(BaseUploadResult.Ok()); }));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("hello", "note.txt"));

        cut.WaitForAssertion(() => Assert.Contains("檔案格式不符", cut.Find(".base-upload-error").TextContent));
        Assert.True(cut.Find(".base-upload-actions fluent-button").HasAttribute("disabled"));
        Assert.False(called);
    }

    [Fact]
    public void 處理失敗顯示錯誤訊息()
    {
        var cut = Render<BaseFileUpload>(p => p
            .Add(x => x.OnUpload, _ => Task.FromResult(BaseUploadResult.Fail("第 2 列錯誤"))));

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("x", "a.csv"));
        cut.WaitForAssertion(() => Assert.Contains("a.csv", cut.Markup));
        cut.Find(".base-upload-actions fluent-button").Click();

        cut.WaitForAssertion(() => Assert.Contains("第 2 列錯誤", cut.Find(".base-upload-result.error").TextContent));
    }

    [Fact]
    public void 匯入後呼叫的頁面立即更新畫面()
    {
        // 曾發生的問題：OnUpload 中更新了頁面的清單，但頁面沒有重新轉譯，清單不會更新
        var cut = Render<UploadHost>();

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("x", "a.csv"));
        cut.WaitForAssertion(() => Assert.Contains("a.csv", cut.Markup));
        cut.Find(".base-upload-actions fluent-button").Click();

        cut.WaitForAssertion(() => Assert.Equal("1", cut.Find(".imported-count").TextContent));
    }

    [Fact]
    public void 顯示範本下載連結()
    {
        var cut = Render<BaseFileUpload>(p => p
            .Add(x => x.TemplateUrl, "templates/a.csv")
            .Add(x => x.OnUpload, _ => Task.FromResult(BaseUploadResult.Ok())));

        Assert.Equal("templates/a.csv", cut.Find(".base-upload-template").GetAttribute("href"));
    }
}
