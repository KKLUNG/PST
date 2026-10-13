# 使用者管理系統（Blazor）

以 .NET 8 Blazor 製作的使用者帳號管理系統，畫面全部使用共用底層元件庫 [BaseComponents](../BaseComponents/README.md) 組成，同時作為底層元件的使用範例。

## 功能

| 頁面 | 網址 | 說明 |
|------|------|------|
| 登入 | `/Account/Login` | 帳號密碼登入（Cookie 驗證），未登入時進入任何頁面都會導向此頁 |
| 首頁 | `/` | 顯示登入者資訊與功能連結 |
| 使用者維護 | `/users` | 依帳號、電子郵件、姓名、狀態查詢；新增、編輯（對話框）、刪除（確認視窗） |
| 寄件登記（範例畫面） | `/demo/shipment` | 使用所有底層元件的完整輸入畫面：寄件人、收件人、郵件資訊、批次匯入；儲存後顯示「登記內容確認」供核對輸入結果（資料存在 CMSData） |
| 底層元件展示 | `/dev/components` | 所有基礎元件的外觀、用法與鍵盤操作示範；可儲存、載入、修改、刪除（資料存在 CMSData） |

規則：不可刪除或停用目前登入的帳號；編輯時密碼留空則維持原密碼；帳號建立後不可修改。

快速鍵：查詢 `Alt+Y`、重設 `Alt+C`、新增 `Alt+A`、儲存 `Alt+Y`、取消 `Alt+N`、登入 `Alt+L`。欄位中按 Enter 跳到下一欄（詳見 BaseComponents 說明）。

## 執行前需要安裝

- .NET 8 SDK
- SQL Server 2016 以上（或 LocalDB）
- SQL Server Management Studio 或 `sqlcmd`（執行建資料庫腳本用）

## 執行步驟

### 1. 建立資料庫

執行 `Data/CreateDatabase.sql`：

```powershell
sqlcmd -S 伺服器名稱 -U 帳號 -P 密碼 -f 65001 -i Data\CreateDatabase.sql
```

> ⚠️ 若伺服器上已有名為 `BlazorUserManagement` 的資料庫，腳本會**先刪除再重建**。

再執行 `Data/CreateCommonTables.sql` 建立共用資料表（參數檔 `Parameters`、地址 `AddressAreas`，含範例資料）：

```powershell
sqlcmd -S 伺服器名稱 -U 帳號 -P 密碼 -d BlazorUserManagement -f 65001 -i Data\CreateCommonTables.sql
```

> 此腳本**可重複執行**：資料表不存在才建立、資料不存在才新增，不會刪除既有資料。已有資料庫的環境只需執行這一支。
> 地址範例資料僅含臺北市、新北市、臺中市、高雄市部分行政區，正式使用請匯入中華郵政公布的完整郵遞區號資料。

### 2. 設定連線字串

`appsettings.json` 含資料庫帳號密碼，**不納入版本控制**。第一次使用時：

```powershell
Copy-Item appsettings.sample.json appsettings.json
```

再將 `appsettings.json` 中的 `YOUR_USER_ID`、`YOUR_PASSWORD` 改為實際帳號密碼。

### 3. 執行

以 Visual Studio 開啟上一層的 `PST.sln`，將 `BlazorUserManagement` 設為啟動專案後按 `F5`；或：

```powershell
dotnet run --project BlazorUserManagement.csproj
```

### 4. 登入

| 帳號 | 密碼 |
|------|------|
| admin | admin123 |
| user1 | admin123 |
| user2 | admin123 |

> 正式環境請刪除或修改測試帳號。

## 目錄結構

```
PST/
├─ PST.sln                       方案檔（元件庫 + 本系統）
├─ BaseComponents/               共用底層元件庫（說明見其 README.md）
└─ files/                        本系統
   ├─ Components/
   │  ├─ App.razor               HTML 外框、轉譯模式設定
   │  ├─ Routes.razor            路由與登入檢查
   │  ├─ Layout/                 MainLayout（頁首、麵包屑、選單）、NavMenu、LoginLayout
   │  └─ Pages/
   │     ├─ Account/Login.razor  登入頁（靜態轉譯）
   │     ├─ Users/UserList.razor       使用者維護（查詢、清單、刪除）
   │     ├─ Users/UserEditDialog.razor 新增／編輯對話框
   │     ├─ Demo/ShipmentRegister.razor 寄件登記（範例畫面）
   │     ├─ Dev/ComponentGallery.razor 底層元件展示
   │     ├─ Home.razor           首頁
   │     └─ Error.razor          錯誤頁
   ├─ Data/                      實體、DbContext、CreateDatabase.sql、CreateCommonTables.sql（參數檔、地址、CMSData）、Cms/（CMSData 存取）
   ├─ Services/                  AuthService（登入驗證）、UserService（使用者 CRUD）、CommonDataService（參數檔、地址資料，供底層元件使用）
   ├─ wwwroot/app.css            版面樣式（元件樣式在 BaseComponents）
   ├─ Models/ShipmentForm.cs     寄件登記表單與驗證規則
   ├─ wwwroot/templates/         匯入範本檔（users-sample.csv、shipments-sample.csv）
   ├─ Program.cs                 啟動設定、服務註冊、登出端點
   └─ appsettings.sample.json    設定檔範本（不含帳密）
```

## 新增一個功能頁

1. 在 `Components/Pages/` 新增 `.razor`，加上 `@page "/網址"` 與 `@attribute [Authorize]`。
2. 用 `<SectionContent SectionName="breadcrumb">` 設定麵包屑。
3. 以 `BaseQueryPanel`、`BaseDataGrid`、`BaseFormDialog` 等底層元件組成畫面（可參考 `Users/`）。
4. 在 `Components/Layout/NavMenu.razor` 加入選單連結。

## 測試資料暫存表 CMSData

各畫面共用的測試資料表，以 `PageName` 區分是哪個畫面的資料（由 `CreateCommonTables.sql` 建立）。
目前「底層元件展示」（`ComponentGallery`）與「寄件登記」（`ShipmentRegister`）的資料都存在這裡。

| 欄位 | 型別 | 用途 |
|------|------|------|
| `PKey` | UNIQUEIDENTIFIER | 主鍵（循序 GUID） |
| `PageName` | NVARCHAR(256) | 畫面名稱 |
| `SH01`–`SH06` | NVARCHAR(MAX) | 長文字（備註、說明） |
| `SX01`–`SX64` | NVARCHAR(1024) | 一般文字；數字、是否也以文字存放 |
| `DE01`–`DE08` | DATETIME | 日期時間 |
| `DA01`–`DA08` | DATE | 日期（不含時間） |
| `CreateDate` | DATETIME | 建立時間 |
| `ModifiedDate` | DATETIME | 修改時間（未修改為 NULL） |

存放規則（由 `Data/Cms/CmsMapper.cs` 轉換，不受系統語系影響）：

| 資料 | 存在 SX 的格式 |
|------|----------------|
| 數字 | `1234567.89`（無千分位、小數點為「.」、去除尾端 0） |
| 是否 | `Y` / `N` |
| 日期時間 | `2026-10-13 14:05:06` |

### 新畫面使用 CMSData

1. 表單模型的屬性標註對應欄位，並定義畫面名稱：

   ```csharp
   public class MyForm
   {
       public const string CmsPageName = "MyPage";

       [CmsColumn("SX01")] public string? Name { get; set; }
       [CmsColumn("SX02")] public decimal? Amount { get; set; }
       [CmsColumn("DA01")] public DateTime? StartDate { get; set; }
       [CmsColumn("SH01")] public string? Remark { get; set; }
   }
   ```

2. 頁面注入 `CmsDataService` 存取：

   ```csharp
   var pkey = await CmsData.SaveAsync(MyForm.CmsPageName, form);          // 新增
   await CmsData.SaveAsync(MyForm.CmsPageName, form, pkey);              // 修改
   var list = await CmsData.ListAsync<MyForm>(MyForm.CmsPageName);       // 清單
   var record = await CmsData.GetAsync<MyForm>(pkey);                    // 讀取一筆
   await CmsData.DeleteAsync(pkey);                                      // 刪除
   await CmsData.AddRangeAsync(MyForm.CmsPageName, forms);               // 一次新增多筆
   ```

3. 在 `tests/BlazorUserManagement.Tests/CmsTests.cs` 的 `CmsFormMappingTests` 加上對應檢查。
   欄位名稱打錯、同一欄位對應兩個屬性、日期欄位對應到非日期屬性時，測試會失敗。

## 自訂

- **公司名稱／Logo**：`Components/Layout/MainLayout.razor` 與 `LoginLayout.razor` 中的 `公司名稱`
- **版面配色**：`wwwroot/app.css` 開頭的色彩變數；元件配色見 BaseComponents 說明

## 備註

- 登入頁採靜態伺服器端轉譯，才能在回應中寫入登入 Cookie，因此使用 Blazor 內建的 `InputText` 而非 Fluent UI 元件；其餘頁面為互動式伺服器端。
- `AuthService` 仍保留產生 JWT 的程式碼，目前登入改用 Cookie，JWT 未被使用。
- Razor Pages 版本保留於 git 的 `razor-pages` 分支。
