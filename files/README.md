# 使用者管理系統（ASP.NET Core Razor Pages）

以 ASP.NET Core 8 Razor Pages 製作的使用者帳號管理系統，包含登入、使用者查詢、新增、編輯、刪除。

## 功能

| 頁面 | 網址 | 說明 |
|------|------|------|
| 登入 | `/Account/Login` | 帳號密碼登入（Cookie 驗證），未登入時進入任何頁面都會導向此頁 |
| 首頁 | `/` | 顯示登入者資訊與功能連結 |
| 使用者查詢 | `/Users` | 依帳號、電子郵件、姓名、狀態查詢；可編輯、刪除 |
| 使用者新增 | `/Users/Edit` | 建立新帳號（密碼必填） |
| 使用者編輯 | `/Users/Edit/{id}` | 修改資料；密碼留空則維持原密碼；帳號不可修改 |

快速鍵：查詢 `Alt+Q`、清除 `Alt+C`、新增 `Alt+A`、存檔 `Alt+S`、返回 `Alt+R`。

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

### 2. 設定連線字串

`appsettings.json` 含資料庫帳號密碼，**不納入版本控制**。第一次使用時：

```powershell
Copy-Item appsettings.sample.json appsettings.json
```

再將 `appsettings.json` 中的 `YOUR_USER_ID`、`YOUR_PASSWORD` 改為實際帳號密碼。

### 3. 執行

Visual Studio 按 `F5`，或：

```powershell
dotnet run
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
├─ Data/
│  ├─ User.cs                  使用者實體、查詢條件、表單模型
│  ├─ ApplicationDbContext.cs  EF Core 資料庫上下文
│  └─ CreateDatabase.sql       建立資料庫腳本（含測試帳號）
├─ Services/
│  ├─ AuthService.cs           登入驗證、BCrypt 密碼加密
│  └─ UserService.cs           使用者查詢、新增、修改、刪除
├─ Pages/
│  ├─ Shared/_Layout.cshtml    系統版面（頁首、麵包屑、左側選單）
│  ├─ Shared/_LoginLayout.cshtml  登入頁版面（無選單）
│  ├─ Account/Login、Logout    登入／登出
│  ├─ Users/Index              使用者查詢
│  ├─ Users/Edit               使用者新增／編輯
│  ├─ Index                    首頁
│  └─ Error                    錯誤頁
├─ wwwroot/css/site.css        版面樣式
├─ Program.cs                  啟動設定、服務註冊
├─ appsettings.sample.json     設定檔範本（不含帳密）
└─ BlazorUserManagement.csproj
```

## 自訂

- **公司名稱／Logo**：`Pages/Shared/_Layout.cshtml` 與 `_LoginLayout.cshtml` 中的 `公司名稱`
- **配色**：`wwwroot/css/site.css` 開頭的 `:root` 色彩變數
- **左側選單**：`Pages/Shared/_Layout.cshtml` 的 `<nav class="sys-menu">`

## 備註

- 專案名稱 `BlazorUserManagement` 沿用自原始範例，目前已改為 Razor Pages，未使用 Blazor。
- `AuthService` 仍保留產生 JWT 的程式碼，目前登入改用 Cookie，JWT 未被使用。
