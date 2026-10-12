# BaseComponents 共用底層元件庫

以 [Microsoft Fluent UI Blazor](https://www.fluentui-blazor.net/)（v4.14.4，免費、MIT 授權）為基礎包裝的 Blazor 元件庫。
目的是讓各系統的輸入欄位、查詢畫面、清單、表單、對話框**寫法一致、外觀一致、操作一致**，新畫面只要組合元件即可。

- 命名慣例：底層元件一律以 `Base` 開頭（同 Vue 官方風格指南對基礎元件的建議）。
- 頁面只使用 `Base` 元件與本庫的型別（`BaseOption`、`BaseIcon`、`BaseTextType`…），不直接使用 Fluent UI。
  日後升級或更換 UI 元件庫時，只需修改本專案。（例外：清單欄位直接使用 Fluent UI 的 `PropertyColumn`／`TemplateColumn`）
- 操作慣例沿用既有系統（POST_CT202206-PH2 的 Component 專案）：按鈕快速鍵依文字自動帶入、Enter 跳下一欄、輸入滿長度自動跳欄。

## 安裝到新系統

1. 專案加入參考：

   ```xml
   <ProjectReference Include="..\BaseComponents\BaseComponents.csproj" />
   ```

2. `Program.cs` 註冊服務：

   ```csharp
   builder.Services.AddBaseComponents();
   ```

3. `App.razor` 引用樣式與鍵盤操作腳本（`{專案名稱}.styles.css` 內含 Fluent UI 元件樣式，**必須引用**）：

   ```html
   <link rel="stylesheet" href="{專案名稱}.styles.css" />
   <link rel="stylesheet" href="_content/BaseComponents/base-components.css" />
   ...
   <script src="_framework/blazor.web.js"></script>
   <script src="_content/BaseComponents/base-components.js"></script>
   ```

4. `_Imports.razor` 加入：

   ```razor
   @using Microsoft.FluentUI.AspNetCore.Components
   @using BaseComponents
   ```

5. `MainLayout.razor` 放一次提供者：

   ```razor
   <BaseProviders />
   ```

> 元件需要**互動模式**（`InteractiveServer`）。靜態轉譯的頁面（例如要寫入 Cookie 的登入頁）只能使用 `BaseField` 排版，輸入框請用 Blazor 內建的 `InputText`。

## 元件一覽

完整示範見系統的「開發工具 > 底層元件展示」頁（`/dev/components`）。

### 基礎輸入元件

| 元件 | 用途 | 主要參數 |
|------|------|----------|
| `BaseTextBox` | 文字方框 | `Type`（Text／Password／Email…）、`MaxLength`、`AutoNext`、`UpperCase`、`Width` |
| `BaseTextArea` | 多行文字 | `Rows`、`MaxLength` |
| `BaseSelect<TValue>` | 下拉式選單 | `Options`、`AllText`（加「全部」選項）、`Placeholder` |
| `BaseNumeric<TValue>` | 數字方框 | `Min`、`Max`、`ShowStep` |
| `BaseDatePicker` | 日期（可輸入或點日曆） | `Roc`（民國年）、`Min`、`Max` |
| `BaseCheckBox` | 核取方塊 | `Label` |
| `BaseRadioGroup<TValue>` | 單選按鈕群組 | `Options`、`Vertical` |
| `BaseButton` | 按鈕 | `Kind`（Default／Primary／Danger）、`Icon`、`AccessKey` |

共通：
- 皆支援 `@bind-Value`，以及 `ReadOnly`、`Disabled`。
- 放在 `EditForm`（或 `BaseFormDialog`）內時，DataAnnotations 驗證訊息會**自動顯示在欄位下方**。

### 專用輸入元件

| 元件 | 用途 | 主要參數 |
|------|------|----------|
| `BaseParameterSelect` | 參數檔下拉選單（依參數群組讀取選項） | `SetItem`、`ShowCode`（顯示「A - 名稱」）、`Exclude`、`Filter`、`AllText` |
| `BaseDateRange` | 起訖日期區間 | `@bind-StartValue`、`@bind-EndValue`、`MaxMonths`（區間上限）、`Roc` |
| `BaseMobile` | 手機（09XX－XXXXXX） | 綁定值 `0912-345678`；模型加 `[BaseMobile]` 檢查格式 |
| `BaseTelephone` | 市話（區碼－號碼#分機） | 綁定值 `02-23456789#123`；`ShowExtension`；模型加 `[BaseTelephone]` |
| `BaseAddress` | 地址（縣市→鄉鎮市區→郵遞區號 + 詳細地址） | `@bind-County`、`@bind-District`、`@bind-ZipCode`、`@bind-Detail`、`FullAddressChanged` |
| `BaseFileUpload` | 檔案上傳（批次匯入） | `Accept`、`Multiple`、`MaxFileSizeMB`、`TemplateUrl`、`OnUpload` |

說明：
- **起訖日期**：自動檢查「起日不可大於迄日」與區間長度。在表單中錯誤會阻擋送出；在查詢區（非表單）請於查詢前呼叫 `@ref` 的 `Validate()`。
- **手機、電話**：只能輸入數字，滿長度自動跳到下一段。格式驗證以屬性加在資料模型（空值視為通過，必填另加 `[Required]`）：

  ```csharp
  [BaseMobile] public string? Mobile { get; set; }
  [BaseTelephone] public string? Tel { get; set; }
  ```

- **地址**：驗證（例如 `[Required]`）加在詳細地址欄位；`BaseAddress.Format(縣市, 區, 詳細)` 可組合完整地址。
- **檔案上傳**：檢查副檔名與大小後才可匯入；`OnUpload` 收到檔案內容，回傳 `BaseUploadResult.Ok(...)` 或 `Fail(...)` 顯示結果：

  ```razor
  <BaseFileUpload Accept=".csv" TemplateUrl="templates/users.csv" OnUpload="ImportAsync" />

  @code {
      private async Task<BaseUploadResult> ImportAsync(IReadOnlyList<BaseUploadFile> files)
      {
          // files[i].Name、Size、Content (byte[])
          return BaseUploadResult.Ok($"匯入完成，共 {count} 筆");
      }
  }
  ```

### 系統需提供的資料來源

參數檔下拉選單與地址元件的資料來自資料庫，但底層元件**不直接存取資料庫**，由系統實作介面並註冊：

| 介面 | 方法 | 供哪個元件使用 |
|------|------|----------------|
| `IBaseParameterProvider` | `GetOptionsAsync(setItem)`：回傳參數群組的選項（代碼、名稱） | `BaseParameterSelect` |
| `IBaseAddressProvider` | `GetCountiesAsync()`、`GetDistrictsAsync(county)`：縣市、鄉鎮市區與郵遞區號 | `BaseAddress` |

```csharp
// Program.cs (範例實作見 files/Services/CommonDataService.cs，含 10 分鐘快取)
builder.Services.AddMemoryCache();
builder.Services.AddScoped<CommonDataService>();
builder.Services.AddScoped<IBaseParameterProvider>(sp => sp.GetRequiredService<CommonDataService>());
builder.Services.AddScoped<IBaseAddressProvider>(sp => sp.GetRequiredService<CommonDataService>());
```

### 版面與複合元件

| 元件 | 用途 |
|------|------|
| `BaseProviders` | 主題色、對話框、訊息通知提供者（Layout 放一次） |
| `BaseField` | 欄位（標籤 + 內容），放在查詢區、表單內自動排成表格式版面；`Required` 顯示 *，`ColSpan` 橫跨多組 |
| `BaseQueryPanel` | 查詢條件區 + 查詢(Y)／重設(C) |
| `BaseDataGrid<TGridItem>` | 資料清單：筆數、工具列、分頁、無資料訊息；`ColumnWidths` 設定欄寬 |
| `BaseFormDialog` | 表單對話框：欄位版面、資料驗證、儲存(Y)／取消(N) |

| 服務 | 用途 |
|------|------|
| `BaseDialogService` | `ConfirmAsync` 確認視窗、`ShowFormAsync<TDialog, TData>` 開啟表單對話框 |
| `BaseNotifier` | `Success`／`Info`／`Warning`／`Error` 訊息通知 |

## 操作慣例

### 按鈕快速鍵

未指定 `AccessKey` 時依按鈕文字自動帶入，顯示為「查詢(Y)」，按 **Alt + 字母** 觸發：

| 文字 | 快速鍵 | 文字 | 快速鍵 |
|------|--------|------|--------|
| 查詢 | Y | 儲存 | Y |
| 新增 | A | 確定 | Y |
| 重設 | C | 取消 | N |
| 返回 | X | 匯出 | O |
| 匯入 | I | | |
| 列印、報表列印、清單列印 | R | 資料列印 | S |

- 自訂：`AccessKey="P"`；不使用：`AccessKey=""`（例如清單每一列的編輯、刪除按鈕）。
- 按鈕執行中會自動停用並顯示處理中，避免重複點擊。

### 鍵盤輸入

在 `BaseQueryPanel`、`BaseFormDialog`（或自行加上 `class="base-keyboard-nav"` 的表單）內：

- 欄位中按 **Enter** 跳到下一個欄位；最後一個欄位按 Enter 移到主要按鈕（查詢、儲存），再按 Enter 即執行。
- `BaseTextBox` 設定 `MaxLength` 時，**輸入滿長度自動跳到下一欄**（適合代碼、條碼機）；`AutoNext="false"` 可關閉。
- 下拉選單展開時 Enter 為選取項目；多行文字中 Enter 為換行。

## 範例

### 輸入表單

```razor
<EditForm Model="model" OnValidSubmit="SaveAsync" class="base-keyboard-nav">
    <DataAnnotationsValidator />
    <div class="base-field-grid">
        <BaseField Label="局號" Required="true">
            <BaseTextBox @bind-Value="model.BranchNo" MaxLength="6" Width="120px" />
        </BaseField>
        <BaseField Label="單位" Required="true">
            <BaseSelect @bind-Value="model.Unit" Options="unitOptions" />
        </BaseField>
        <BaseField Label="數量">
            <BaseNumeric @bind-Value="model.Qty" Min="1" Max="9999" />
        </BaseField>
        <BaseField Label="日期">
            <BaseDatePicker @bind-Value="model.Date" Roc="true" />
        </BaseField>
    </div>
    <div class="base-button-bar">
        <BaseButton Text="儲存" Kind="BaseButtonKind.Primary" Icon="BaseIcon.Save" ButtonType="ButtonType.Submit" />
    </div>
</EditForm>

@code {
    private static readonly BaseOption<string?>[] unitOptions =
    [
        new("A", "A - 台北郵局"),
        new("B", "B - 台中郵局"),
    ];
}
```

### 查詢維護畫面

完整範例見 `files/Components/Pages/Users/`（使用者維護）。

```razor
<BaseQueryPanel OnSearch="SearchAsync" OnReset="ResetAsync" Busy="loading">
    <BaseField Label="帳號"><BaseTextBox @bind-Value="query.Username" /></BaseField>
    <BaseField Label="狀態">
        <BaseSelect @bind-Value="query.IsActive" Options="statusOptions" AllText="－ 全部 －" />
    </BaseField>
</BaseQueryPanel>

<BaseDataGrid Items="users" Loading="loading" ColumnWidths="1fr 2fr 200px">
    <ToolBarContent>
        <BaseButton Text="新增" Kind="BaseButtonKind.Primary" Icon="BaseIcon.Add" OnClick="CreateAsync" />
    </ToolBarContent>
    <Columns>
        <PropertyColumn Property="x => x.Username" Title="帳號" Sortable="true" />
        <PropertyColumn Property="x => x.CreatedDate" Title="建立日期" Format="yyyy/MM/dd" Align="Align.Center" />
        <TemplateColumn Title="操作" Align="Align.Center" Context="user">
            <BaseButton Text="編輯" AccessKey="" Icon="BaseIcon.Edit" OnClick="() => EditAsync(user)" />
            <BaseButton Text="刪除" AccessKey="" Kind="BaseButtonKind.Danger" Icon="BaseIcon.Delete" OnClick="() => DeleteAsync(user)" />
        </TemplateColumn>
    </Columns>
</BaseDataGrid>

@code {
    private async Task CreateAsync()
    {
        if (await Dialog.ShowFormAsync<UserEditDialog, UserEditArgs>("使用者新增", new UserEditArgs(null)))
            await SearchAsync();
    }

    private async Task DeleteAsync(UserDto user)
    {
        if (!await Dialog.ConfirmAsync($"確認要刪除「{user.Username}」嗎？", "刪除確認", "刪除"))
            return;
        // 刪除後 Notifier.Success("已刪除")
    }
}
```

### 表單對話框

```razor
@* UserEditDialog.razor *@
@implements IDialogContentComponent<UserEditArgs>

<BaseFormDialog Model="model" OnSave="SaveAsync">
    <BaseField Label="帳號" Required="true"><BaseTextBox @bind-Value="model.Username" /></BaseField>
    <BaseField Label="狀態"><BaseCheckBox @bind-Value="model.IsActive" Label="啟用" /></BaseField>
    <BaseField Label="備註" ColSpan="2"><BaseTextArea @bind-Value="model.Remark" /></BaseField>
</BaseFormDialog>

@code {
    // 由 BaseDialogService.ShowFormAsync 傳入的資料 (需為類別，例如 record UserEditArgs(int? UserId))
    [Parameter] public UserEditArgs Content { get; set; } = default!;

    // 驗證通過後呼叫；回傳 true 關閉對話框，false 保持開啟 (例如帳號重複)
    private async Task<bool> SaveAsync() { ... }
}
```

## 自訂外觀

- **主題色**：`<BaseProviders AccentColor="#0f6cbd" />`（預設綠色 `#4f8f26`）
- **欄位表格、清單配色**：覆寫 `base-components.css` 開頭的 CSS 變數（`--base-green-main`、`--base-label-bg`…）
- **輸入框寬度**：各元件的 `Width` 參數，例如 `Width="120px"`、`Width="100%"`

## 擴充新元件

1. 在本專案新增 `BaseXxx.razor`，內部使用 Fluent UI 元件並固定公司慣用的參數（尺寸、文字、行為）。
2. 只開放各系統真正需要調整的參數，且參數型別使用本庫自己的型別，不要讓 Fluent UI 的型別出現在頁面上。
3. 需要 `@bind-Value` 時提供 `Value`、`ValueChanged`、`ValueExpression` 三個參數；
   並參考既有元件，在 `EditContext` 存在時加上 `<FluentValidationMessage For="@ValueExpression" />`。
4. 樣式加在 `wwwroot/base-components.css`，類別名稱以 `base-` 開頭。
5. 在「底層元件展示」頁加上示範，並在本文件的元件一覽補上說明。

## 版本與升級

- 目前使用 Fluent UI Blazor **4.14.4**（穩定版）。
- Fluent UI Blazor 5.x 已推出（2026/09），屬大版本更新、API 有異動；待其穩定後再評估升級，升級時只需修改本專案。
