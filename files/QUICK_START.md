# 🚀 快速開始指南

## 5 分鐘快速部署

### 步驟 1️⃣ 建立資料庫 (2 分鐘)

**SQL Server Management Studio (SSMS):**
1. 連接到您的 SQL Server 實例
2. 開啟新的 Query 視窗
3. 複製並執行 `CreateDatabase.sql` 中的所有 SQL 語句
4. 驗證 `BlazorUserManagement` 資料庫已建立

**使用 PowerShell:**
```powershell
sqlcmd -S YOUR_SERVER -U sa -P YOUR_PASSWORD -i CreateDatabase.sql
```

---

### 步驟 2️⃣ 建立 Blazor 專案 (2 分鐘)

**在 Visual Studio 2022 中:**

1. **建立新專案**
   - 選擇「Blazor Server App (.NET 8)」
   - 專案名稱: `BlazorUserManagement`
   - 位置: 選擇您的工作目錄
   - Framework: `.NET 8.0`

2. **替換專案檔案**
   - 用提供的 `BlazorUserManagement.csproj` 替換原有檔案
   - 用提供的 `Program.cs` 替換原有檔案

3. **建立資料夾結構**
   ```
   BlazorUserManagement/
   ├── Data/
   ├── Services/
   ├── Components/
   │   ├── Account/
   │   ├── User/
   │   └── Layout/
   ├── wwwroot/
   └── Properties/
   ```

4. **複製檔案到對應位置**
   - 資料層: `User.cs`, `ApplicationDbContext.cs` → `Data/`
   - 服務層: `AuthService.cs`, `UserService.cs` → `Services/`
   - 元件層: `Login.razor`, `UserList.razor`, `UserEdit.razor`, `ConfirmDialog.razor` → `Components/`
   - 應用層: `App.razor` → 根目錄
   - 設定: `appsettings.json` → 根目錄

---

### 步驟 3️⃣ 安裝 NuGet 套件 (1 分鐘)

**套件管理器主控台 (Package Manager Console):**
```powershell
# 進入專案目錄
cd BlazorUserManagement

# 安裝必要套件
dotnet add package MudBlazor
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package BCrypt.Net-Next
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package Microsoft.IdentityModel.Tokens
dotnet add package Serilog.AspNetCore

# 或使用 NuGet 套件管理員 GUI (PM> 指令)
Install-Package MudBlazor
Install-Package Microsoft.EntityFrameworkCore.SqlServer
Install-Package BCrypt.Net-Next
```

---

### 步驟 4️⃣ 設定連接字串

編輯 `appsettings.json`：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=BlazorUserManagement;Integrated Security=true;TrustServerCertificate=true;"
  },
  "Jwt": {
    "SecretKey": "your-very-long-secret-key-minimum-32-characters-required-for-security-12345",
    "Issuer": "BlazorUserManagementApp",
    "Audience": "BlazorUserManagementUsers",
    "ExpirationMinutes": 60
  }
}
```

**連接字串範例:**
```
# Windows 驗證 (推薦開發環境)
Server=LAPTOP\SQLEXPRESS;Database=BlazorUserManagement;Integrated Security=true;TrustServerCertificate=true;

# SQL Server 驗證
Server=192.168.1.100;Database=BlazorUserManagement;User Id=sa;Password=YourPassword;TrustServerCertificate=true;

# 命名實例
Server=MYCOMPUTER\SQLSERVER2016;Database=BlazorUserManagement;Integrated Security=true;TrustServerCertificate=true;
```

---

### 步驟 5️⃣ 執行應用 (1 分鐘)

**在 Visual Studio 中:**
1. 按下 `F5` 開始偵錯
2. 應用會自動在 `https://localhost:7XXX` 開啟
3. 系統會自動應用資料庫遷移

**使用命令列:**
```bash
dotnet run
```

---

## ✅ 測試登入

應用啟動後，進入登入頁面：
```
URL: https://localhost:7XXX/login

帳號1:
  使用者名稱: admin
  密碼: admin123

帳號2:
  使用者名稱: user1
  密碼: admin123
```

登入後可訪問以下頁面：
- `/users` - 使用者管理清單
- `/login` - 登入頁面

---

## 🛠️ 如遇到問題

### 問題 1: 無法連接 SQL Server

**症狀**: `A network-related or instance-specific error...`

**解決方案**:
```powershell
# 1. 檢查 SQL Server 服務是否運行
Get-Service MSSQLSERVER | Select Status

# 2. 啟動服務
Start-Service MSSQLSERVER

# 3. 驗證連接字串中的伺服器名稱
# 使用: SqlServer\SQLEXPRESS 或 localhost
```

### 問題 2: 編譯錯誤 - 缺少命名空間

**症狀**: `The type or namespace name 'MudBlazor' could not be found`

**解決方案**:
```bash
# 清潔並重建專案
dotnet clean
dotnet build
```

### 問題 3: 端口已被佔用

**症狀**: `Address already in use`

**解決方案**:
```bash
# 尋找佔用特定埠的程序
netstat -ano | findstr :7XXX

# 終止程序
taskkill /PID <PID> /F

# 或改用不同埠
dotnet run --urls "https://localhost:7777"
```

### 問題 4: 資料庫遷移失敗

**症狀**: `The database does not exist`

**解決方案**:
```bash
# 手動建立資料庫
sqlcmd -S YOUR_SERVER -Q "CREATE DATABASE BlazorUserManagement"

# 或執行提供的 SQL 腳本
```

---

## 📚 目錄結構說明

```
BlazorUserManagement/
│
├─ Data/                          # 資料層
│  ├─ User.cs                     # 使用者實體 & DTO 模型
│  ├─ ApplicationDbContext.cs      # EF Core 資料庫上下文
│  └─ [自動生成] Migrations/      # 資料庫遷移檔案
│
├─ Services/                       # 業務邏輯層
│  ├─ AuthService.cs              # 登入驗證服務
│  └─ UserService.cs              # 使用者 CRUD 服務
│
├─ Components/                     # UI 元件層
│  ├─ Account/
│  │  └─ Login.razor              # 登入元件
│  ├─ User/
│  │  ├─ UserList.razor           # 使用者清單
│  │  ├─ UserEdit.razor           # 編輯對話框
│  │  └─ ConfirmDialog.razor      # 確認對話框
│  └─ Layout/
│     ├─ MainLayout.razor         # 主版面配置
│     └─ NavMenu.razor            # 導航選單
│
├─ wwwroot/                       # 靜態檔案
│  ├─ css/
│  ├─ js/
│  └─ images/
│
├─ Properties/
│  └─ launchSettings.json         # VS 啟動設定
│
├─ Program.cs                      # 應用進入點 & 依賴注入
├─ App.razor                       # 根元件
├─ appsettings.json               # 應用設定
├─ appsettings.Development.json    # 開發環境設定
└─ BlazorUserManagement.csproj     # 專案檔案

SQL/
└─ CreateDatabase.sql             # 資料庫初始化腳本
```

---

## 🎓 適合作為教學案例的重點

### 1. **Blazor 框架基礎**
   - 元件開發 (.razor 檔案)
   - 資料綁定 (@bind)
   - 事件處理 (@onclick, @onsubmit)
   - 條件渲染 (@if, @foreach)

### 2. **MudBlazor 元件庫使用**
   - TextField, Button, Dialog
   - MudTable 資料表格
   - Snackbar 通知
   - 主題化 (Theme)

### 3. **後端架構設計**
   - 分層設計 (Data/Service/Component)
   - 依賴注入 (DI)
   - Entity Framework Core ORM
   - CRUD 操作

### 4. **資料庫設計與最佳實踐**
   - 資料表設計
   - 約束與索引
   - 檢視表與預存程序
   - 資料遷移

### 5. **安全機制**
   - 密碼加密 (BCrypt)
   - JWT 驗證
   - 表單驗證
   - 異常處理

### 6. **軟體工程實踐**
   - 編碼命名規範
   - 異常處理策略
   - 日誌記錄
   - 版本控制

---

## 📖 參考資源

### 官方文檔
- [Microsoft Blazor 文檔](https://learn.microsoft.com/aspnet/core/blazor)
- [MudBlazor 元件庫](https://www.mudblazor.com/)
- [Entity Framework Core](https://learn.microsoft.com/ef/core/)

### 教學視頻
- Blazor Server 基礎教學
- MudBlazor 元件使用教學
- SQL Server 基礎管理

### 相關技術主題
- C# 現代特性 (LINQ, async/await)
- ASP.NET Core 中間件
- 前端 HTML/CSS/JavaScript 基礎

---

**祝您開發愉快！如有問題，歡迎提出。** 🎉
