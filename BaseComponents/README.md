# BaseComponents 共用底層元件庫

以 [MudBlazor](https://mudblazor.com/)（免費、MIT 授權）為基礎包裝的 Blazor 元件庫。
目的是讓各系統的查詢畫面、清單、表單、對話框**寫法一致、外觀一致**，新畫面只要組合元件即可。

> 命名慣例：底層元件一律以 `Base` 開頭（同 Vue 官方風格指南對基礎元件的建議）。

## 安裝到新系統

1. 專案加入參考：

   ```xml
   <ProjectReference Include="..\BaseComponents\BaseComponents.csproj" />
   ```

2. `Program.cs` 註冊服務：

   ```csharp
   builder.Services.AddBaseComponents();
   ```

3. `App.razor` 的 `<head>` 引用樣式，`<body>` 引用 MudBlazor 腳本：

   ```html
   <link rel="stylesheet" href="_content/MudBlazor/MudBlazor.min.css" />
   <link rel="stylesheet" href="_content/BaseComponents/base-components.css" />
   ...
   <script src="_content/MudBlazor/MudBlazor.min.js"></script>
   ```

4. `_Imports.razor` 加入：

   ```razor
   @using MudBlazor
   @using BaseComponents
   ```

5. `MainLayout.razor` 放一次提供者：

   ```razor
   <BaseProviders />
   ```

> 元件需要**互動模式**（`InteractiveServer`）。靜態轉譯的頁面（例如要寫入 Cookie 的登入頁）只能使用 `BaseField` 排版，輸入框請用 Blazor 內建的 `InputText`。

## 元件一覽

| 元件 | 用途 |
|------|------|
| `BaseProviders` | 主題、對話框、訊息通知提供者（Layout 放一次） |
| `BaseButton` | 按鈕：一般／主要／危險，`AccessKey` 自動顯示「(Q)」並支援 Alt 快速鍵 |
| `BaseQueryPanel` | 查詢條件區：表格式欄位 + 查詢(Q)／清除(C)，按 Enter 即查詢 |
| `BaseField` | 欄位（標籤 + 內容），放在 `BaseQueryPanel`、`BaseFormDialog` 內 |
| `BaseTextBox` | 文字輸入框（Text／Password／Email） |
| `BaseSelect<TValue>` | 下拉選單，可加「全部」選項 |
| `BaseCheckBox` | 核取方塊 |
| `BaseDataGrid<T>` | 資料清單：筆數、工具列、分頁、無資料訊息 |
| `BaseFormDialog` | 表單對話框：欄位版面、資料驗證、存檔(S)／取消 |

| 服務 | 用途 |
|------|------|
| `BaseDialogService` | `ConfirmAsync` 確認視窗、`ShowFormAsync<T>` 開啟表單對話框 |
| `BaseNotifier` | `Success`／`Info`／`Warning`／`Error` 訊息通知 |

## 範例：一個完整的查詢維護畫面

完整範例見 `files/Components/Pages/Users/`（使用者維護）。

### 查詢頁

```razor
@page "/users"
@inject UserService UserService
@inject BaseDialogService Dialog
@inject BaseNotifier Notifier

<BaseQueryPanel OnSearch="SearchAsync" OnClear="ClearAsync" Busy="loading">
    <BaseField Label="帳號"><BaseTextBox @bind-Value="query.Username" /></BaseField>
    <BaseField Label="狀態">
        <BaseSelect @bind-Value="query.IsActive" Options="statusOptions" AllText="－ 全部 －" />
    </BaseField>
</BaseQueryPanel>

<BaseDataGrid Items="users" Loading="loading">
    <ToolBarContent>
        <BaseButton Text="新增" AccessKey="A" Kind="BaseButtonKind.Primary" OnClick="CreateAsync" />
    </ToolBarContent>
    <Columns>
        <PropertyColumn Property="x => x.Username" Title="帳號" />
        <PropertyColumn Property="x => x.CreatedDate" Title="建立日期" Format="yyyy/MM/dd" />
        <TemplateColumn Title="操作" Sortable="false">
            <CellTemplate>
                <BaseButton Text="編輯" OnClick="() => EditAsync(context.Item)" />
                <BaseButton Text="刪除" Kind="BaseButtonKind.Danger" OnClick="() => DeleteAsync(context.Item)" />
            </CellTemplate>
        </TemplateColumn>
    </Columns>
</BaseDataGrid>

@code {
    private static readonly BaseOption<bool?>[] statusOptions = [new(true, "啟用"), new(false, "停用")];

    private async Task CreateAsync()
    {
        if (await Dialog.ShowFormAsync<UserEditDialog>("使用者新增"))
            await SearchAsync();
    }

    private async Task DeleteAsync(UserDto user)
    {
        if (!await Dialog.ConfirmAsync($"確認要刪除「{user.Username}」嗎？", "刪除確認", "刪除", danger: true))
            return;
        // 刪除後 Notifier.Success("已刪除")
    }
}
```

### 表單對話框

```razor
@* UserEditDialog.razor *@
<BaseFormDialog Model="model" OnSave="SaveAsync">
    <BaseField Label="帳號" Required="true"><BaseTextBox @bind-Value="model.Username" /></BaseField>
    <BaseField Label="電子郵件" Required="true"><BaseTextBox @bind-Value="model.Email" InputType="InputType.Email" /></BaseField>
    <BaseField Label="狀態"><BaseCheckBox @bind-Value="model.IsActive" Label="啟用" /></BaseField>
    <BaseField Label="備註" ColSpan="2"><BaseTextBox @bind-Value="model.Remark" /></BaseField>
</BaseFormDialog>

@code {
    [Parameter] public int? UserId { get; set; }

    // 驗證通過後呼叫；回傳 true 關閉對話框，false 保持開啟 (例如帳號重複)
    private async Task<bool> SaveAsync() { ... }
}
```

- `model` 的 DataAnnotations（`[Required]`、`[EmailAddress]`…）會自動驗證，錯誤顯示在欄位下方。
- `ColSpan="2"` 讓欄位橫跨整列。
- `Columns` 參數可改變每列欄位組數（預設 2）。

## 自訂外觀

- **配色**：覆寫 `base-components.css` 開頭的 CSS 變數（`--base-green-main`、`--base-label-bg`…）
- **MudBlazor 主題**：`<BaseProviders Theme="myTheme" />` 傳入自訂的 `MudTheme`，預設見 `BaseTheme.cs`

## 擴充新元件

1. 在本專案新增 `BaseXxx.razor`，內部使用 MudBlazor 元件並固定公司慣用的參數（尺寸、樣式、文字）。
2. 只開放各系統真正需要調整的參數；需要 `@bind-Value` 時提供 `Value`、`ValueChanged`、`ValueExpression` 三個參數。
3. 樣式加在 `wwwroot/base-components.css`，類別名稱以 `base-` 開頭。
4. 在本文件的元件一覽補上說明。
