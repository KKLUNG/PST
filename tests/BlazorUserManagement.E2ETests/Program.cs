// 瀏覽器端對端測試：以 Edge 實際操作 Blazor 互動畫面，並直接查詢資料庫確認結果
// 由 tests/run-e2e.ps1 執行 (會先重設 LocalDB 測試資料庫並啟動系統)
// 參數：[截圖資料夾]；環境變數 E2E_BASE_URL、E2E_DB 可覆寫網址與連線字串
using Microsoft.Data.SqlClient;
using Microsoft.Playwright;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var BaseUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5199";
var Db = Environment.GetEnvironmentVariable("E2E_DB")
    ?? @"Server=(localdb)\MSSQLLocalDB;Database=BlazorUserManagement;Integrated Security=true;TrustServerCertificate=true;";
var shotDir = args.Length > 0 ? args[0] : Path.Combine(Path.GetTempPath(), "pst-e2e-shots");
Directory.CreateDirectory(shotDir);
int pass = 0, fail = 0;

void Check(string name, bool ok, string detail = "")
{
    if (ok) { pass++; Console.WriteLine($"  ✓ {name}"); }
    else { fail++; Console.WriteLine($"  ✗ {name} {detail}"); }
}

object? Sql(string sql)
{
    using var conn = new SqlConnection(Db);
    conn.Open();
    using var cmd = new SqlCommand(sql, conn);
    return cmd.ExecuteScalar();
}

using var pw = await Playwright.CreateAsync();
await using var browser = await pw.Chromium.LaunchAsync(new() { Channel = "msedge", Headless = true });
var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1300, Height = 900 }, Locale = "zh-TW" });
page.SetDefaultTimeout(10000);
page.PageError += (_, err) => Console.WriteLine($"  [page error] {err}");

// 依 BaseField 標籤找到同列的內容區
ILocator Cell(string label, ILocator? scope = null) =>
    (scope ?? page.Locator("body")).Locator($"xpath=.//div[contains(@class,'base-field-label')][starts-with(normalize-space(.),'{label}')]/following-sibling::div[1]").First;
// 文字方框內的 input (CSS 選擇器可穿透 Fluent 元件的 Shadow DOM)
ILocator Field(string label, ILocator? scope = null) => Cell(label, scope).Locator("input").First;
ILocator Button(string name, ILocator? scope = null) =>
    (scope ?? page.Locator("body")).GetByRole(AriaRole.Button, new() { Name = name });
ILocator Dialog() => page.Locator("fluent-dialog").Last;
async Task<bool> ToastHas(string text)
{
    try { await page.Locator(".fluent-toast-provider, [class*=toast]").GetByText(text).First.WaitForAsync(new() { Timeout = 5000 }); return true; }
    catch (TimeoutException) { return false; }
}
async Task<string> Count() => await page.Locator(".base-grid-count strong").InnerTextAsync();
async Task WaitCount(string n)
{
    try { await page.Locator(".base-grid-count strong", new() { HasText = n }).WaitForAsync(new() { Timeout = 5000 }); } catch (TimeoutException) { }
}
async Task SelectOption(string label, string option, ILocator? scope = null)
{
    await Cell(label, scope).Locator("fluent-select").ClickAsync();
    await Cell(label, scope).Locator("fluent-option", new() { HasText = option }).ClickAsync();
}
async Task SetCheckbox(ILocator scope, bool check)
{
    var box = scope.Locator("fluent-checkbox").First;
    var isChecked = await box.GetAttributeAsync("aria-checked") == "true";
    if (isChecked != check) await box.ClickAsync();
}
async Task<string> ActiveTag() => await page.EvaluateAsync<string>("() => { const a = document.activeElement; return a ? a.tagName.toLowerCase() + (a.dataset.basePrimary ? '[primary]' : '') + (a.dataset.uppercase ? '[upper]' : '') : ''; }");
async Task<string> GalleryValues() => await page.Locator(".gallery-values").InnerTextAsync();
async Task<bool> Eventually(Func<Task<bool>> condition, int timeoutMs = 4000)
{
    var until = DateTime.Now.AddMilliseconds(timeoutMs);
    while (true)
    {
        try { if (await condition()) return true; } catch (PlaywrightException) { }
        if (DateTime.Now > until) return false;
        await Task.Delay(150);
    }
}
Task<bool> ValuesHave(string text) => Eventually(async () => (await GalleryValues()).Contains(text));

Console.WriteLine("[1] 登入");
await page.GotoAsync($"{BaseUrl}/users");
Check("未登入導向登入頁", page.Url.Contains("/Account/Login?ReturnUrl=%2Fusers"), page.Url);
await page.Locator("input[autocomplete=username]").FillAsync("admin");
await page.Locator("input[type=password]").FillAsync("wrong");
await page.GetByRole(AriaRole.Button, new() { Name = "登入(L)" }).ClickAsync();
Check("密碼錯誤顯示錯誤訊息", await page.Locator(".msg-error").InnerTextAsync() == "使用者名稱或密碼不正確");
await page.Locator("input[autocomplete=username]").FillAsync("admin");
await page.Locator("input[type=password]").FillAsync("admin123");
await page.GetByRole(AriaRole.Button, new() { Name = "登入(L)" }).ClickAsync();
await page.WaitForURLAsync($"{BaseUrl}/users");
Check("登入成功並導回 /users", page.Url == $"{BaseUrl}/users", page.Url);
await WaitCount("3");
Check("清單顯示 3 筆", await Count() == "3", await Count());
Check("頁首顯示登入者", (await page.Locator(".sys-user-name").InnerTextAsync()).Contains("admin - 系統管理員"));

Console.WriteLine("[2] 查詢");
await Field("姓名").FillAsync("李");
await Button("查詢(Y)").ClickAsync();
await WaitCount("1");
Check("姓名查「李」得 1 筆", await Count() == "1", await Count());
Check("結果為 user1", (await page.Locator("table.base-grid").InnerTextAsync()).Contains("user1@company.com"));
Check("按鈕快速鍵屬性 查詢=y、重設=c、新增=a",
    await page.Locator("fluent-button[data-base-primary]").First.GetAttributeAsync("accesskey") == "y"
    && await page.Locator("fluent-button[accesskey=c]").CountAsync() == 1
    && await page.Locator("fluent-button[accesskey=a]").CountAsync() == 1);
await Button("重設(C)").ClickAsync();
await WaitCount("3");
Check("重設後回到 3 筆", await Count() == "3", await Count());
Check("重設後欄位清空", await Field("姓名").InputValueAsync() == "");
await SelectOption("狀態", "停用");
await Button("查詢(Y)").ClickAsync();
await WaitCount("0");
Check("狀態選「停用」查得 0 筆", await Count() == "0", await Count());
Check("顯示無資料訊息", await page.Locator(".base-grid-empty", new() { HasText = "查無符合條件的資料" }).IsVisibleAsync());
await Button("重設(C)").ClickAsync();
await WaitCount("3");
await Field("帳號").FocusAsync();
await page.Keyboard.PressAsync("Enter");
Check("查詢欄位按 Enter 跳下一欄 (電子郵件)", await page.EvaluateAsync<bool>("() => document.activeElement?.closest('.base-field-content') === document.querySelectorAll('.base-field-content')[1]"));

Console.WriteLine("[3] 新增");
await Button("新增(A)").ClickAsync();
var dlg = Dialog();
await dlg.Locator("fluent-button").First.WaitForAsync();
Check("開啟新增對話框", (await dlg.InnerTextAsync()).Contains("使用者新增"));
await Field("帳號", dlg).FillAsync("test1");
await Field("電子郵件", dlg).FillAsync("bad-email");
await Field("姓名", dlg).FillAsync("測試人員");
await Button("儲存(Y)", dlg).ClickAsync();
Check("Email 格式錯誤顯示欄位訊息", await dlg.GetByText("電子郵件格式不正確").IsVisibleAsync());
await page.ScreenshotAsync(new() { Path = Path.Combine(shotDir, "dialog-validation.png") });
await Field("電子郵件", dlg).FillAsync("test1@company.com");
await Button("儲存(Y)", dlg).ClickAsync();
Check("未填密碼顯示錯誤", await ToastHas("新增使用者時必須設定密碼"));
await Field("密碼", dlg).FillAsync("pass1234");
await SetCheckbox(Cell("狀態", dlg), false);
await Button("儲存(Y)", dlg).ClickAsync();
Check("新增成功訊息", await ToastHas("已新增"));
await dlg.WaitForAsync(new() { State = WaitForSelectorState.Detached });
await WaitCount("4");
Check("新增後清單 4 筆", await Count() == "4", await Count());
Check("資料庫中 test1 為停用", Convert.ToInt32(Sql("SELECT IsActive FROM Users WHERE Username='test1'")) == 0);
Check("資料庫中姓名中文正確", (string?)Sql("SELECT FullName FROM Users WHERE Username='test1'") == "測試人員");
await Button("新增(A)").ClickAsync();
dlg = Dialog();
await Field("帳號", dlg).FillAsync("user1");
await Field("電子郵件", dlg).FillAsync("x@company.com");
await Field("密碼", dlg).FillAsync("pass1234");
await Button("儲存(Y)", dlg).ClickAsync();
Check("帳號重複顯示錯誤、對話框不關閉", await ToastHas("已存在") && await dlg.Locator("fluent-button").First.IsVisibleAsync());
await Button("取消(N)", dlg).ClickAsync();
await dlg.WaitForAsync(new() { State = WaitForSelectorState.Detached });

Console.WriteLine("[4] 編輯");
var id = Convert.ToInt32(Sql("SELECT UserId FROM Users WHERE Username='test1'"));
var hash = (string?)Sql($"SELECT PasswordHash FROM Users WHERE UserId={id}");
var row = page.Locator("table.base-grid tr", new() { HasText = "test1@company.com" });
await Button("編輯", row).ClickAsync();
dlg = Dialog();
await dlg.Locator("fluent-button").First.WaitForAsync();
await Field("電子郵件", dlg).WaitForAsync();
Check("編輯對話框帶出原資料", await Field("電子郵件", dlg).InputValueAsync() == "test1@company.com");
Check("帳號唯讀", await Cell("帳號", dlg).Locator("fluent-text-field").GetAttributeAsync("readonly") != null);
await page.ScreenshotAsync(new() { Path = Path.Combine(shotDir, "dialog-edit.png") });
await Field("姓名", dlg).FillAsync("已修改");
await SetCheckbox(Cell("狀態", dlg), true);
await Button("儲存(Y)", dlg).ClickAsync();
Check("更新成功訊息", await ToastHas("已更新"));
await dlg.WaitForAsync(new() { State = WaitForSelectorState.Detached });
Check("資料庫：姓名已修改、狀態啟用", (string?)Sql($"SELECT FullName + ',' + CAST(IsActive AS varchar) FROM Users WHERE UserId={id}") == "已修改,1");
Check("密碼留空維持原密碼", (string?)Sql($"SELECT PasswordHash FROM Users WHERE UserId={id}") == hash);
Check("清單即時更新", await Eventually(async () => (await page.Locator("table.base-grid").InnerTextAsync()).Contains("已修改")));
row = page.Locator("table.base-grid tr", new() { HasText = "admin@company.com" });
await Button("編輯", row).ClickAsync();
dlg = Dialog();
await Field("電子郵件", dlg).WaitForAsync();
await SetCheckbox(Cell("狀態", dlg), false);
await Button("儲存(Y)", dlg).ClickAsync();
Check("不可停用自己", await ToastHas("不可停用目前登入的帳號"));
await Button("取消(N)", dlg).ClickAsync();
await dlg.WaitForAsync(new() { State = WaitForSelectorState.Detached });

Console.WriteLine("[5] 刪除");
row = page.Locator("table.base-grid tr", new() { HasText = "admin@company.com" });
await Button("刪除", row).ClickAsync();
Check("不可刪除自己", await ToastHas("不可刪除目前登入的帳號"));
row = page.Locator("table.base-grid tr", new() { HasText = "test1@company.com" });
await Button("刪除", row).ClickAsync();
dlg = Dialog();
await dlg.Locator("fluent-button").First.WaitForAsync();
Check("顯示刪除確認", (await dlg.InnerTextAsync()).Contains("確認要刪除使用者「test1」嗎"));
await page.ScreenshotAsync(new() { Path = Path.Combine(shotDir, "dialog-confirm.png") });
await Button("取消", dlg).ClickAsync();
await dlg.WaitForAsync(new() { State = WaitForSelectorState.Detached });
Check("取消刪除後資料仍在", Convert.ToInt32(Sql($"SELECT COUNT(*) FROM Users WHERE UserId={id}")) == 1);
await Button("刪除", row).ClickAsync();
dlg = Dialog();
await Button("刪除", dlg).ClickAsync();
Check("刪除成功訊息", await ToastHas("已刪除"));
await WaitCount("3");
Check("資料庫中 test1 已刪除", Convert.ToInt32(Sql($"SELECT COUNT(*) FROM Users WHERE UserId={id}")) == 0);
Check("清單回到 3 筆", await Count() == "3", await Count());

Console.WriteLine("[6] 底層元件展示頁");
await page.GetByRole(AriaRole.Link, new() { Name = "底層元件展示" }).ClickAsync();
await page.WaitForURLAsync($"{BaseUrl}/dev/components");
await Field("帳號").WaitForAsync();
await Button("儲存(Y)").ClickAsync();
Check("必填驗證訊息 (帳號、局號、單位、日期)", await Eventually(async () => { var body = await page.Locator(".base-keyboard-nav").InnerTextAsync(); return body.Contains("請輸入帳號") && body.Contains("請輸入局號") && body.Contains("請選擇單位") && body.Contains("請選擇日期"); }), await page.Locator(".base-keyboard-nav").InnerTextAsync());
await Field("帳號").FillAsync("demo");
await Field("帳號").PressAsync("Enter");
Check("Enter 跳到下一欄 (局號)", await page.EvaluateAsync<bool>("() => document.activeElement?.closest('.base-field-content')?.previousElementSibling?.textContent.includes('局號')"));
await page.Keyboard.TypeAsync("123456");
Check("局號輸入滿 6 碼自動跳到產品代碼", await Eventually(async () => (await ActiveTag()).Contains("[upper]")), await ActiveTag());
await page.Keyboard.TypeAsync("abcd1234");   // 模擬條碼機連續快速輸入 8 碼 (滿長度自動跳欄)
await page.Keyboard.PressAsync("Tab");
Check("產品代碼快速輸入自動轉大寫且不掉字", await ValuesHave("\"ProductCode\": \"ABCD1234\""), await GalleryValues());
await SelectOption("單位", "B - 台中郵局");
Check("下拉選單選取", await ValuesHave("\"Unit\": \"B\""));
await Field("數量").FillAsync("0");
await Field("數量").PressAsync("Tab");
Check("數量 0 超出範圍：顯示驗證訊息或自動修正為 1", await Eventually(async () => await page.GetByText("數量須介於 1-9999").IsVisibleAsync() || (await GalleryValues()).Contains("\"Qty\": 1,")), await GalleryValues());
await page.WaitForTimeoutAsync(400);   // 等前一次修正 (0 → 1) 回到畫面後再輸入
await Field("數量").FillAsync("12");
await Field("數量").PressAsync("Tab");
Check("數量綁定為數字", await ValuesHave("\"Qty\": 12"));

// ---------- 數字千分位 ----------
var amount = Field("金額");
await amount.FocusAsync();
await page.Keyboard.TypeAsync("1234567.891");   // 第 3 位小數會被擋下
Check("金額小數超過 2 位的輸入被擋下", await amount.InputValueAsync() == "1234567.89", await amount.InputValueAsync());
await page.Keyboard.TypeAsync("abc");
Check("金額不可輸入文字", await amount.InputValueAsync() == "1234567.89", await amount.InputValueAsync());
await amount.PressAsync("Tab");
Check("離開欄位顯示千分位 1,234,567.89", await Eventually(async () => await amount.InputValueAsync() == "1,234,567.89"), await amount.InputValueAsync());
Check("金額綁定為數字", await ValuesHave("\"Amount\": 1234567.89"));
await amount.FocusAsync();
Check("進入欄位顯示原始數字方便修改", await Eventually(async () => await amount.InputValueAsync() == "1234567.89"), await amount.InputValueAsync());
await page.Keyboard.PressAsync("Tab");
Check("未修改離開時恢復千分位", await Eventually(async () => await amount.InputValueAsync() == "1,234,567.89"), await amount.InputValueAsync());
await Field("日期（西元）").FillAsync("2026/10/12");
await Field("日期（西元）").PressAsync("Tab");
Check("西元日期輸入", await ValuesHave("\"AdDate\": \"2026-10-12T00:00:00\""), await GalleryValues());
await Field("日期（民國）").FillAsync("115/10/13");
await Field("日期（民國）").PressAsync("Tab");
Check("民國日期輸入 115/10/13 → 2026-10-13", await ValuesHave("\"RocDate\": \"2026-10-13T00:00:00\""), await GalleryValues());
Check("民國日期顯示為 115/10/13", await Field("日期（民國）").InputValueAsync() == "115/10/13", await Field("日期（民國）").InputValueAsync());
await page.WaitForTimeoutAsync(300);
await Field("日期（民國）").FillAsync("2026/10/14");   // 誤把西元年輸入到民國年欄位
await Field("日期（民國）").PressAsync("Tab");
Check("民國欄位誤輸入西元 2026/10/14 自動修正為 115/10/14", await ValuesHave("\"RocDate\": \"2026-10-14T00:00:00\""), await GalleryValues());
Check("修正後顯示民國年", await Eventually(async () => await Field("日期（民國）").InputValueAsync() == "115/10/14"), await Field("日期（民國）").InputValueAsync());
await Cell("性別").Locator("fluent-radio", new() { HasText = "女" }).ClickAsync();
Check("單選按鈕", await ValuesHave("\"Gender\": \"F\""));
await Cell("同意").Locator("fluent-checkbox").ClickAsync();
Check("核取方塊", await ValuesHave("\"Agree\": true"));

// ---------- 參數檔下拉選單 ----------
await Cell("付款方式").Locator("fluent-select").ClickAsync();
var payOptions = await Cell("付款方式").Locator("fluent-option").AllInnerTextsAsync();
Check("參數檔選項來自資料庫 (含代碼)", payOptions.Any(o => o.Contains("2 - 信用卡")) && payOptions.Any(o => o.Contains("4 - 郵政劃撥")), string.Join("|", payOptions));
await Cell("付款方式").Locator("fluent-option", new() { HasText = "2 - 信用卡" }).ClickAsync();
Check("參數檔選取", await ValuesHave("\"PayType\": \"2\""));
await Cell("單位（參數檔）").Locator("fluent-select").ClickAsync();
var unitOptions = await Cell("單位（參數檔）").Locator("fluent-option").AllInnerTextsAsync();
Check("參數檔不顯示已刪除、ShowCode=false 不含代碼", unitOptions.Count == 4 && unitOptions.Contains("台北郵局") && !unitOptions.Any(o => o.Contains("花蓮")), string.Join("|", unitOptions));
await page.Keyboard.PressAsync("Escape");

// ---------- 起訖日期 ----------
var startDate = Cell("查詢期間").Locator("input").Nth(0);
var endDate = Cell("查詢期間").Locator("input").Nth(1);
await page.WaitForTimeoutAsync(400);
await startDate.FillAsync("115/10/12");
await startDate.PressAsync("Tab");
await page.WaitForTimeoutAsync(400);
await endDate.FillAsync("115/10/01");
await endDate.PressAsync("Tab");
Check("起日大於迄日顯示錯誤", await Eventually(() => Cell("查詢期間").GetByText("起日不可大於迄日").IsVisibleAsync()));
await page.WaitForTimeoutAsync(400);
await endDate.FillAsync("116/02/01");
await endDate.PressAsync("Tab");
Check("超過 3 個月顯示錯誤", await Eventually(() => Cell("查詢期間").GetByText("請重新設定").IsVisibleAsync()));
await page.WaitForTimeoutAsync(400);
await endDate.FillAsync("115/12/31");
await endDate.PressAsync("Tab");
Check("區間正確後錯誤消失、值已綁定",
    await Eventually(async () => !await Cell("查詢期間").GetByText("請重新設定").IsVisibleAsync())
    && await ValuesHave("\"StartDate\": \"2026-10-12T00:00:00\"") && await ValuesHave("\"EndDate\": \"2026-12-31T00:00:00\""));

// ---------- 手機、電話 ----------
var mobile = Cell("手機").Locator("input");
await mobile.Nth(0).FocusAsync();
await page.Keyboard.TypeAsync("0912");
Check("手機前 4 碼滿自動跳到後段", await Eventually(() => mobile.Nth(1).EvaluateAsync<bool>("el => el.getRootNode().activeElement === el")));
await page.Keyboard.TypeAsync("34a5x678");
Check("手機只接受數字、組成 0912-345678", await ValuesHave("\"Mobile\": \"0912-345678\""), await GalleryValues());
await mobile.Nth(1).FillAsync("345");
await mobile.Nth(1).PressAsync("Tab");
Check("手機格式錯誤顯示訊息", await Eventually(() => Cell("手機").GetByText("手機號碼格式應為").IsVisibleAsync()));
await mobile.Nth(1).FillAsync("345678");
var tel = Cell("電話").Locator("input");
await tel.Nth(0).FillAsync("02");
await tel.Nth(1).FocusAsync();
await page.Keyboard.TypeAsync("23456789");
Check("電話號碼滿 8 碼自動跳到分機", await Eventually(() => tel.Nth(2).EvaluateAsync<bool>("el => el.getRootNode().activeElement === el")));
await page.Keyboard.TypeAsync("123");
Check("電話組成 02-23456789#123", await ValuesHave("\"Tel\": \"02-23456789#123\""), await GalleryValues());

// ---------- 地址 ----------
var addressSelects = Cell("地址").Locator("fluent-select");
await addressSelects.Nth(0).ClickAsync();
await addressSelects.Nth(0).Locator("fluent-option", new() { HasText = "臺北市" }).ClickAsync();
await addressSelects.Nth(1).ClickAsync();
var districtOptions = await addressSelects.Nth(1).Locator("fluent-option").AllInnerTextsAsync();
Check("選縣市後載入鄉鎮市區", districtOptions.Contains("大安區") && !districtOptions.Contains("板橋區"), string.Join("|", districtOptions));
await addressSelects.Nth(1).Locator("fluent-option", new() { HasText = "大安區" }).ClickAsync();
Check("選鄉鎮市區自動帶出郵遞區號 106", await ValuesHave("\"ZipCode\": \"106\"") && await Cell("地址").GetByText("106").IsVisibleAsync());
await Cell("地址").Locator("fluent-text-field input").FillAsync("忠孝東路一段1號");
Check("完整地址組合", await ValuesHave("\"FullAddress\": \"臺北市大安區忠孝東路一段1號\""), await GalleryValues());
await addressSelects.Nth(0).ClickAsync();
await addressSelects.Nth(0).Locator("fluent-option", new() { HasText = "新北市" }).ClickAsync();
Check("更換縣市清空鄉鎮市區與郵遞區號", await ValuesHave("\"District\": null") && await ValuesHave("\"ZipCode\": null"));
await addressSelects.Nth(1).ClickAsync();
await addressSelects.Nth(1).Locator("fluent-option", new() { HasText = "板橋區" }).ClickAsync();
Check("新北市板橋區郵遞區號 220", await ValuesHave("\"ZipCode\": \"220\""));

// ---------- 最後一欄 Enter → 儲存 ----------
await Cell("地址").Locator("fluent-text-field input").FocusAsync();
await page.Keyboard.PressAsync("Enter");
Check("最後一欄按 Enter 移到儲存按鈕", await Eventually(async () => (await ActiveTag()).Contains("[primary]")), await ActiveTag());
await page.Keyboard.PressAsync("Enter");
Check("在儲存按鈕按 Enter 送出、驗證通過", await ToastHas("驗證通過"), await page.Locator(".base-keyboard-nav").InnerTextAsync());

// ---------- 檔案上傳 ----------
var tmpDir = Path.Combine(Path.GetTempPath(), "uitest-upload");
Directory.CreateDirectory(tmpDir);
var goodCsv = Path.Combine(tmpDir, "good.csv");
var badCsv = Path.Combine(tmpDir, "bad.csv");
var txtFile = Path.Combine(tmpDir, "note.txt");
File.WriteAllText(goodCsv, "帳號,電子郵件,姓名\nu1,u1@a.com,甲\nu2,u2@a.com,乙\nu3,u3@a.com,丙\n");
File.WriteAllText(badCsv, "帳號,電子郵件,姓名\nu1,,甲\n");
File.WriteAllText(txtFile, "hello");
var fileInput = page.Locator(".base-upload input[type=file]");
Check("範本下載連結", await page.Locator(".base-upload-template").GetAttributeAsync("href") == "templates/users-sample.csv");
Check("匯入按鈕未選檔前停用", await Button("匯入(I)").IsDisabledAsync());
await fileInput.SetInputFilesAsync(new[] { txtFile });
Check("副檔名不符顯示錯誤", await Eventually(() => page.Locator(".base-upload").GetByText("檔案格式不符").IsVisibleAsync()));
await fileInput.SetInputFilesAsync(new[] { goodCsv });
Check("選檔後列出檔名", await Eventually(() => page.Locator(".base-upload-files").GetByText("good.csv").IsVisibleAsync()));
await Button("匯入(I)").ClickAsync();
Check("匯入成功顯示結果", await Eventually(() => page.Locator(".base-upload-result.success").GetByText("檢查通過，共 3 筆").IsVisibleAsync()));
await fileInput.SetInputFilesAsync(new[] { badCsv });
await Eventually(() => page.Locator(".base-upload-files").GetByText("bad.csv").IsVisibleAsync());
await Button("匯入(I)").ClickAsync();
Check("匯入失敗顯示錯誤列", await Eventually(() => page.Locator(".base-upload-result.error").GetByText("第 2 列").IsVisibleAsync()));
var templateStatus = await page.APIRequest.GetAsync($"{BaseUrl}/templates/users-sample.csv");
Check("範本檔可下載", templateStatus.Status == 200);
await page.ScreenshotAsync(new() { Path = Path.Combine(shotDir, "gallery.png"), FullPage = true });
await Button("處理中示範").ClickAsync();
Check("執行中按鈕自動停用", await Eventually(() => Button("處理中示範").IsDisabledAsync(), 1500));
Check("處理完成後恢復", await ToastHas("處理完成") && await Eventually(async () => !await Button("處理中示範").IsDisabledAsync()));

Console.WriteLine("[7] 寄件登記（範例畫面）");
await page.GetByRole(AriaRole.Link, new() { Name = "寄件登記（範例）" }).ClickAsync();
await page.WaitForURLAsync($"{BaseUrl}/demo/shipment");
var sender = page.Locator(".base-field-grid").Nth(0);
var receiver = page.Locator(".base-field-grid").Nth(1);
var mail = page.Locator(".base-field-grid").Nth(2);
await Field("姓名", sender).WaitForAsync();
Check("寄件日期預設為今天", await Field("寄件日期", mail).InputValueAsync() == BaseRoc(DateTime.Today), await Field("寄件日期", mail).InputValueAsync());

await Button("儲存(Y)").ClickAsync();
var formText = await page.Locator(".base-keyboard-nav").InnerTextAsync();
Check("未填資料顯示必填訊息", await Eventually(async () =>
{
    var t = await page.Locator(".base-keyboard-nav").InnerTextAsync();
    return t.Contains("請輸入寄件人姓名") && t.Contains("請輸入收件人手機") && t.Contains("請選擇郵件種類") && t.Contains("請輸入重量");
}), formText);

async Task PickAddressAsync(ILocator scope, string county, string district, string detail)
{
    var selects = Cell("地址", scope).Locator("fluent-select");
    await selects.Nth(0).ClickAsync();
    await selects.Nth(0).Locator("fluent-option", new() { HasText = county }).ClickAsync();
    await selects.Nth(1).ClickAsync();
    await selects.Nth(1).Locator("fluent-option", new() { HasText = district }).ClickAsync();
    await Cell("地址", scope).Locator("fluent-text-field input").FillAsync(detail);
}

async Task TypeInto(ILocator field, string text)
{
    await field.FocusAsync();
    await page.Keyboard.TypeAsync(text);
    await page.Keyboard.PressAsync("Tab");
}

// 寄件人 (先不填手機、電話，檢查跨欄位規則)
await Field("姓名", sender).FillAsync("王小明");
await PickAddressAsync(sender, "臺北市", "大安區", "忠孝東路一段1號");
// 收件人
await Field("姓名", receiver).FillAsync("李大華");
var receiverMobile = Cell("手機", receiver).Locator("input");
await receiverMobile.Nth(0).FocusAsync();
await page.Keyboard.TypeAsync("0922111222");   // 前 4 碼滿自動跳到後段
await PickAddressAsync(receiver, "新北市", "板橋區", "文化路一段100號");
// 郵件資訊
await Cell("郵件種類", mail).Locator("fluent-select").ClickAsync();
await Cell("郵件種類", mail).Locator("fluent-option", new() { HasText = "2 - 快捷郵件" }).ClickAsync();
await Cell("付款方式", mail).Locator("fluent-select").ClickAsync();
await Cell("付款方式", mail).Locator("fluent-option", new() { HasText = "1 - 現金" }).ClickAsync();
await TypeInto(Field("件數", mail), "2");
await TypeInto(Field("重量", mail), "1.2505");   // 小數第 4 位被擋下
await TypeInto(Field("保價金額", mail), "12345");

await Button("儲存(Y)").ClickAsync();
Check("跨欄位規則：寄件人手機、電話至少填一項", await Eventually(() => page.GetByText("寄件人手機、電話至少填寫一項").IsVisibleAsync()));

var senderMobile = Cell("手機", sender).Locator("input");
await senderMobile.Nth(0).FocusAsync();
await page.Keyboard.TypeAsync("0912345678");
await page.Keyboard.PressAsync("Tab");
await page.WaitForTimeoutAsync(300);
await Button("儲存(Y)").ClickAsync();
Check("儲存成功", await ToastHas("第 1 筆寄件已登記"), await page.Locator(".base-keyboard-nav").InnerTextAsync());

var confirm = page.Locator(".confirm-table");
Check("顯示登記內容確認", await Eventually(() => confirm.IsVisibleAsync()));
var confirmText = await confirm.InnerTextAsync();
string Row(string label) => confirmText.Split('\n').FirstOrDefault(l => l.StartsWith(label + "\t"))?.Split('\t').Last().Trim() ?? "";
Check("確認：寄件人手機", Row("寄件人手機") == "0912-345678", Row("寄件人手機"));
Check("確認：寄件人地址含郵遞區號", Row("寄件人地址") == "106 臺北市大安區忠孝東路一段1號", Row("寄件人地址"));
Check("確認：收件人手機", Row("收件人手機") == "0922-111222", Row("收件人手機"));
Check("確認：收件人地址", Row("收件人地址") == "220 新北市板橋區文化路一段100號", Row("收件人地址"));
Check("確認：郵件種類 (參數檔名稱)", Row("郵件種類") == "快捷郵件", Row("郵件種類"));
Check("確認：付款方式", Row("付款方式") == "現金", Row("付款方式"));
Check("確認：件數", Row("件數") == "2 件", Row("件數"));
Check("確認：重量 (小數 3 位)", Row("重量") == "1.25 公斤", Row("重量"));
Check("確認：保價金額千分位", Row("保價金額") == "NT$ 12,345", Row("保價金額"));
Check("確認：寄件日期民國與西元", Row("寄件日期") == $"{BaseRoc(DateTime.Today)}（西元 {DateTime.Today:yyyy/MM/dd}）", Row("寄件日期"));
Check("確認：寄件方式預設臨櫃", Row("寄件方式") == "臨櫃交寄", Row("寄件方式"));
Check("儲存後表單清空", await Eventually(async () => await Field("姓名", sender).InputValueAsync() == ""));
Check("清單 1 筆", await Count() == "1", await Count());
await page.ScreenshotAsync(new() { Path = Path.Combine(shotDir, "shipment.png"), FullPage = true });

// 批次匯入：下載範本後上傳
var template = await page.APIRequest.GetAsync($"{BaseUrl}/templates/shipments-sample.csv");
Check("寄件範本可下載", template.Status == 200);
var templateFile = Path.Combine(Path.GetTempPath(), "pst-e2e-shipments.csv");
await File.WriteAllBytesAsync(templateFile, await template.BodyAsync());
var shipmentUpload = page.Locator(".base-upload input[type=file]");
await shipmentUpload.SetInputFilesAsync(new[] { templateFile });
await Eventually(() => page.Locator(".base-upload-files").GetByText("pst-e2e-shipments.csv").IsVisibleAsync());
await Button("匯入(I)").ClickAsync();
Check("匯入範本 2 筆", await Eventually(() => page.Locator(".base-upload-result.success").GetByText("共匯入 2 筆").IsVisibleAsync()));
Check("清單共 3 筆", await Eventually(async () => await Count() == "3"), await Count());

var badShipments = Path.Combine(Path.GetTempPath(), "pst-e2e-bad.csv");
await File.WriteAllTextAsync(badShipments, "寄件日期(民國),收件人姓名,收件人手機,收件人地址,件數,重量(公斤)\n116/12/01,甲,0912-345678,臺北市,1,1\n116/12/01,乙,0912345678,臺北市,0,1\n");
await shipmentUpload.SetInputFilesAsync(new[] { badShipments });
await Eventually(() => page.Locator(".base-upload-files").GetByText("pst-e2e-bad.csv").IsVisibleAsync());
await Button("匯入(I)").ClickAsync();
Check("匯入檔有錯誤時整批不匯入並標示列號", await Eventually(() => page.Locator(".base-upload-result.error").GetByText("第 3 列").IsVisibleAsync()));
Check("清單仍為 3 筆", await Count() == "3", await Count());

Console.WriteLine("[8] 選單與登出");
await page.GetByRole(AriaRole.Link, new() { Name = "首頁" }).ClickAsync();
await page.WaitForURLAsync($"{BaseUrl}/");
Check("首頁顯示登入資訊", await Eventually(async () => (await page.Locator(".sys-main").InnerTextAsync()).Contains("系統管理員")));
Check("選單反白目前頁面", await Eventually(async () => await page.Locator(".tree-leaf a.active").InnerTextAsync() == "首頁"));
await page.GetByRole(AriaRole.Button, new() { Name = "登出" }).ClickAsync();
await page.WaitForURLAsync($"{BaseUrl}/Account/Login");
Check("登出後回到登入頁", page.Url == $"{BaseUrl}/Account/Login");
await page.GotoAsync($"{BaseUrl}/users");
Check("登出後無法進入 /users", page.Url.Contains("/Account/Login"), page.Url);

Console.WriteLine($"== 通過 {pass}，失敗 {fail} ==");
return fail == 0 ? 0 : 1;

// 民國年日期 (與 BaseDateFormat.ToRoc 相同格式)
static string BaseRoc(DateTime date) =>
    $"{new System.Globalization.TaiwanCalendar().GetYear(date):000}/{date:MM/dd}";
