# Blazor Server + MudBlazor Web 應用範例
## 包含登入及使用者維護功能

### 專案概述
本專案展示 .NET 8 Blazor Server 應用，整合 MudBlazor UI 元件庫，實現以下功能：
- ✅ 使用者登入驗證
- ✅ 使用者管理 (CRUD 操作)
- ✅ SQL Server 2016 資料庫連接
- ✅ Entity Framework Core ORM
- ✅ 響應式設計

---

## 📋 前置準備

### 系統需求
- Visual Studio 2022 (Community/Professional/Enterprise)
- .NET 8 SDK
- SQL Server 2016 或更新版本
- 至少 2GB 可用磁碟空間

### 建立步驟

#### 1️⃣ 建立資料庫
在 SQL Server 執行以下 T-SQL 腳本：

```sql
-- 建立資料庫
CREATE DATABASE BlazorUserManagement;
GO

USE BlazorUserManagement;
GO

-- 建立使用者資料表
CREATE TABLE Users (
    UserId INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName NVARCHAR(100),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedDate DATETIME,
    LastLoginDate DATETIME
);

-- 建立索引
CREATE INDEX IX_Users_Username ON Users(Username);
CREATE INDEX IX_Users_Email ON Users(Email);

-- 插入測試帳號 (密碼: admin123 - BCrypt 加密)
-- 實際應用中應該在應用程式中加密密碼
INSERT INTO Users (Username, Email, PasswordHash, FullName, IsActive, CreatedDate)
VALUES 
('admin', 'admin@company.com', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcg7b3XeKeUxWdeS86E36XQuvKm', '系統管理員', 1, GETDATE()),
('user1', 'user1@company.com', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcg7b3XeKeUxWdeS86E36XQuvKm', '一般使用者', 1, GETDATE());

GO
```

#### 2️⃣ 在 VS2022 建立專案
```bash
dotnet new blazorserver -n BlazorUserManagement -f net8.0
cd BlazorUserManagement
```

#### 3️⃣ 安裝必要的 NuGet 套件
```bash
dotnet add package MudBlazor
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package BCrypt.Net-Next
```

---

## 📁 專案結構

```
BlazorUserManagement/
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor
│   │   └── NavMenu.razor
│   ├── Account/
│   │   ├── Login.razor          ← 登入頁面
│   │   └── Logout.razor         ← 登出頁面
│   └── User/
│       ├── UserList.razor       ← 使用者列表
│       ├── UserEdit.razor       ← 使用者編輯
│       └── UserForm.razor       ← 使用者表單
├── Data/
│   ├── ApplicationDbContext.cs   ← Entity Framework 上下文
│   ├── User.cs                  ← 使用者實體類別
│   └── AuthService.cs           ← 驗證服務
├── Services/
│   ├── UserService.cs           ← 使用者服務
│   └── JwtTokenService.cs       ← JWT Token 服務
├── App.razor                    ← 應用根元件
├── Program.cs                   ← 應用設定
├── appsettings.json             ← 設定檔
└── appsettings.Development.json ← 開發設定

```

---

## 🔗 連接字串設定

編輯 **appsettings.json**：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=BlazorUserManagement;Integrated Security=true;TrustServerCertificate=true;"
  },
  "Jwt": {
    "SecretKey": "your-very-long-secret-key-minimum-32-characters-required-for-security",
    "Issuer": "BlazorApp",
    "Audience": "BlazorAppUsers",
    "ExpirationMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

> ⚠️ **重要**：
> - 將 `YOUR_SERVER` 替換為您的 SQL Server 實例名稱 (例如: `LAPTOP\SQLEXPRESS`)
> - `Integrated Security=true` 使用 Windows 驗證
> - `TrustServerCertificate=true` 用於開發環境，生產環境應配置正確憑證

---

## 🛠️ 主要程式文件說明

### User.cs - 使用者實體
```csharp
public class User
{
    public int UserId { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string FullName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
}
```

### AuthService.cs - 驗證服務
- 提供登入驗證
- 密碼 BCrypt 雜湊驗證
- JWT Token 產生

### UserService.cs - 使用者業務邏輯
- GetAllUsers() - 取得所有使用者
- GetUserById(id) - 取得單一使用者
- CreateUser(user) - 新增使用者
- UpdateUser(user) - 編輯使用者
- DeleteUser(id) - 刪除使用者

### Login.razor - 登入頁面
- 使用者名稱/密碼登入
- 驗證失敗訊息提示
- 登入成功後導向主頁

---

## 🚀 執行應用

### 方法1：Visual Studio 2022
1. 開啟方案
2. 點選 "Start Debugging" (F5)
3. 應用將在 https://localhost:7xxx 開啟

### 方法2：命令列
```bash
dotnet run
```

### 測試帳號
```
使用者名稱: admin
密碼: admin123

使用者名稱: user1
密碼: admin123
```

---

## 💡 教學重點

本專案適合作為 EMBA 資訊管理課程的案例研究，涵蓋：

1. **現代 Web 框架** - Blazor Server 的實時互動
2. **前端 UI 設計** - MudBlazor 元件的使用
3. **後端架構** - 分層設計 (Data/Services/Components)
4. **資料庫設計** - 正規化與索引
5. **安全機制** - 密碼加密、驗證授權
6. **CRUD 操作** - 完整的資料操作示例

---

## 📚 參考資源

- [Blazor 官方文檔](https://learn.microsoft.com/en-us/aspnet/core/blazor)
- [MudBlazor 元件庫](https://www.mudblazor.com/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [BCrypt.Net-Next](https://github.com/BcryptNet/bcrypt.net-next)

---

## 🔐 安全建議

### 生產環境
- ✅ 使用環境變數儲存敏感資訊 (連接字串、JWT Secret)
- ✅ 實施 HTTPS 強制
- ✅ 設定 CORS 政策
- ✅ 實施速率限制 (Rate Limiting)
- ✅ 記錄稽核日誌
- ✅ 定期更新依賴套件
- ✅ 實施 OWASP Top 10 防護

### 密碼政策
- 最少 8 個字元
- 包含大小寫字母、數字、特殊符號
- 不允許重複使用最近 5 個密碼

---

**版本**: 1.0  
**最後更新**: 2024年10月  
**適用**: .NET 8, SQL Server 2016+, VS 2022
