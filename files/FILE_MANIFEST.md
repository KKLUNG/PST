# 📦 專案檔案清單

本套件包含 **Blazor Server + MudBlazor** 完整的使用者管理系統範例。所有檔案均已準備妥當，可直接在 Visual Studio 2022 中使用。

---

## 📄 檔案清單 (13 個檔案)

### 📖 文件檔案

| 檔案名稱 | 說明 | 優先級 |
|---------|------|-------|
| `README.md` | **重要** - 專案完整說明，包含所有概念、設定與安全建議 | ⭐⭐⭐ |
| `QUICK_START.md` | **必讀** - 5分鐘快速部署指南，逐步教學 | ⭐⭐⭐ |
| `FILE_MANIFEST.md` | 本檔案 - 所有檔案的索引與說明 | ⭐⭐ |

### 💾 資料層檔案 (`Data/` 資料夾)

| 檔案名稱 | 類別 | 說明 |
|---------|------|------|
| `User.cs` | 實體類別 | 使用者實體、DTO 及 Request/Response 模型 |
| `ApplicationDbContext.cs` | DbContext | Entity Framework Core 資料庫上下文 |

### 🔐 服務層檔案 (`Services/` 資料夾)

| 檔案名稱 | 類別 | 說明 |
|---------|------|------|
| `AuthService.cs` | 驗證服務 | 登入驗證、密碼加密、JWT Token 產生 |
| `UserService.cs` | 業務服務 | 使用者 CRUD (Create/Read/Update/Delete) 操作 |

### 🎨 UI 元件檔案 (`Components/` 資料夾)

#### Account 子資料夾
| 檔案名稱 | 說明 |
|---------|------|
| `Login.razor` | 登入頁面，使用 MudBlazor 表單元件 |

#### User 子資料夾
| 檔案名稱 | 說明 |
|---------|------|
| `UserList.razor` | 使用者清單頁面，支援搜尋、新增、編輯、刪除 |
| `UserEdit.razor` | 使用者編輯對話框（新增/編輯模式） |
| `ConfirmDialog.razor` | 通用確認對話框元件 |

### ⚙️ 應用設定檔案

| 檔案名稱 | 說明 |
|---------|------|
| `Program.cs` | **關鍵** - 應用啟動、依賴注入、服務註冊 |
| `App.razor` | 應用根元件、HTML 結構 |
| `appsettings.json` | 應用設定（連接字串、JWT 設定） |

### 📊 資料庫與專案檔案

| 檔案名稱 | 說明 |
|---------|------|
| `CreateDatabase.sql` | SQL Server 資料庫建立腳本（含測試資料） |
| `BlazorUserManagement.csproj` | 專案檔案（NuGet 套件依賴） |

---

## 🎯 使用流程

### 第 1 步：準備資料庫
```
1. 開啟 SQL Server Management Studio
2. 執行 CreateDatabase.sql
3. 驗證資料庫 BlazorUserManagement 已建立
```
**相關檔案**: `CreateDatabase.sql`

### 第 2 步：建立 VS 專案
```
1. VS 2022 → 建立新的 Blazor Server 專案
2. 複製所有程式碼檔案到對應資料夾
3. 更新 appsettings.json 連接字串
```
**相關檔案**: 所有 `.cs` 和 `.razor` 檔案

### 第 3 步：安裝依賴套件
```
1. 套件管理器主控台 → Install-Package MudBlazor
2. 安裝其他必要套件（或參考 .csproj）
```
**相關檔案**: `BlazorUserManagement.csproj`

### 第 4 步：執行應用
```
1. F5 啟動偵錯
2. 開啟 https://localhost:7xxx/login
3. 使用測試帳號登入
```
**相關檔案**: `Program.cs`, `Login.razor`

---

## 🗂️ 檔案與功能對應表

### 登入功能
```
使用者輸入 → Login.razor → AuthService.LoginAsync()
          ↓
      驗證密碼 → User.cs (PasswordHash)
          ↓
    產生 JWT Token → AuthService.GenerateJwtToken()
```
**涉及檔案**: `Login.razor`, `AuthService.cs`, `User.cs`

### 使用者清單
```
頁面載入 → UserList.razor → UserService.GetAllUsersAsync()
        ↓
   查詢資料庫 → ApplicationDbContext.Users
        ↓
    顯示資料表 → MudBlazor MudTable
```
**涉及檔案**: `UserList.razor`, `UserService.cs`, `ApplicationDbContext.cs`

### 新增/編輯使用者
```
開啟對話框 → UserEdit.razor → UserService.CreateUserAsync() / UpdateUserAsync()
           ↓
    驗證資料 → User.cs (Validation Attributes)
           ↓
    加密密碼 → AuthService.HashPassword()
           ↓
    儲存資料庫 → ApplicationDbContext.SaveChangesAsync()
```
**涉及檔案**: `UserEdit.razor`, `UserService.cs`, `AuthService.cs`

### 刪除使用者
```
確認對話框 → ConfirmDialog.razor → UserService.DeleteUserAsync()
           ↓
   刪除記錄 → ApplicationDbContext.Users.Remove()
```
**涉及檔案**: `ConfirmDialog.razor`, `UserService.cs`, `ApplicationDbContext.cs`

---

## 🔧 自訂與擴展指南

### 修改使用者欄位
1. **編輯** `User.cs` - 新增 property
2. **編輯** `ApplicationDbContext.cs` - 在 `OnModelCreating()` 設定
3. **執行遷移** - `Add-Migration AddNewColumn` → `Update-Database`
4. **更新 UI** - 在 `UserEdit.razor` 新增表單欄位

### 新增新頁面
1. 在 `Components/` 建立新的 `.razor` 檔案
2. 加入 `@page "/yourpage"` 指令
3. 在 `NavMenu.razor` 加入導航連結

### 新增新服務
1. 在 `Services/` 建立新的 `.cs` 檔案
2. 在 `Program.cs` 註冊: `builder.Services.AddScoped<YourService>();`
3. 在元件中注入: `@inject YourService Service`

---

## ✅ 快速檢查清單

設定完成後，請確認以下項目：

- [ ] SQL Server 已安裝並運行
- [ ] `BlazorUserManagement` 資料庫已建立
- [ ] VS 2022 專案建立完成
- [ ] 所有檔案複製到正確位置
- [ ] NuGet 套件安裝完成
- [ ] `appsettings.json` 連接字串已更新
- [ ] `F5` 應用成功啟動
- [ ] 能夠使用 admin/admin123 登入
- [ ] 使用者管理頁面能夠正常顯示
- [ ] 能夠新增/編輯/刪除使用者

---

## 📝 檔案修改清單

若您已有基礎 Blazor 專案，以下是需要修改的檔案：

### 需要全部替換的檔案
- `Program.cs` - 新增 MudBlazor、EF Core、驗證服務
- `appsettings.json` - 新增連接字串和 JWT 設定
- `App.razor` - 新增 MudBlazor CSS 參考

### 需要複製的新檔案
- 所有 `Data/` 檔案（新增）
- 所有 `Services/` 檔案（新增）
- 所有 `Components/` 檔案（新增或替換）

### 需要更新的檔案
- `BlazorUserManagement.csproj` - 對應所有 NuGet 套件

---

## 🔐 安全提示

### 生產環境部署前
1. **變更 JWT Secret**: 在 `appsettings.json` 使用強密碼
2. **啟用 HTTPS**: 配置有效的 SSL 憑證
3. **移除測試帳號**: 刪除 `CreateDatabase.sql` 中的測試資料
4. **設定資料庫權限**: 建立專用的 SQL 帳號（參見 SQL 檔案註解）
5. **啟用日誌記錄**: 使用 Serilog 記錄異常
6. **定期備份**: 設定自動備份策略

---

## 📞 常見問題

### Q: 能否在共享主機上部署？
A: 可以，但需要確保：
   - 支援 .NET 8
   - 支援 SQL Server (或改用 SQL Server Express LocalDB)
   - 允許自訂連接字串設定

### Q: 如何變更密碼原則？
A: 編輯 `UserService.cs` 的 `ChangePasswordAsync()` 方法，新增密碼驗證邏輯

### Q: 能否使用 SQLite 而不是 SQL Server？
A: 可以，但需要安裝 `Microsoft.EntityFrameworkCore.Sqlite` 並修改 `Program.cs`

### Q: 如何新增角色權限？
A: 
   1. 在 `User.cs` 新增 `Role` property
   2. 在 UI 新增角色選擇下拉清單
   3. 在服務層新增角色驗證邏輯

---

## 📚 學習資源

### 初級（推薦先學）
- Blazor 基礎 - 元件、資料綁定、事件處理
- MudBlazor 元件庫 - TextField、Button、Table、Dialog 的使用

### 中級（本專案核心）
- Entity Framework Core - DbContext、LINQ、遷移
- 非同步編程 - async/await 模式
- 依賴注入 - DI 容器的使用

### 高級（進階主題）
- JWT 驗證機制
- 密碼加密最佳實踐 (BCrypt)
- SQL Server 效能優化
- 單元測試與整合測試

---

**版本**: 1.0  
**建立日期**: 2024年10月  
**適用平台**: .NET 8, SQL Server 2016+, Visual Studio 2022  
**教學用途**: ✅ EMBA 資訊管理課程

---

**開始使用**: 請先閱讀 `QUICK_START.md` 進行快速部署！ 🚀
