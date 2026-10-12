# PST — Blazor 共用底層元件庫與範例系統

以 **.NET 8 Blazor** 與 **Microsoft Fluent UI Blazor** 建立的共用底層元件庫，以及使用這些元件的範例系統。

| 資料夾 | 內容 | 說明文件 |
|--------|------|----------|
| `BaseComponents/` | 共用底層元件庫（文字方框、下拉選單、數字千分位、民國年日期、起訖日期、手機、電話、地址、檔案上傳、查詢區、清單、表單對話框…） | [BaseComponents/README.md](BaseComponents/README.md) |
| `files/` | 範例系統：登入、使用者維護、寄件登記（範例畫面）、底層元件展示 | [files/README.md](files/README.md) |
| `tests/` | 單元測試、瀏覽器端對端測試 | 見下方「測試」 |
| `PST.sln` | 方案檔（以 Visual Studio 開啟） | |

## 快速開始

1. 建立資料庫：依序執行 `files/Data/CreateDatabase.sql`、`files/Data/CreateCommonTables.sql`
2. 設定連線字串：複製 `files/appsettings.sample.json` 為 `appsettings.json` 並填入帳號密碼
3. 以 Visual Studio 開啟 `PST.sln`，將 `BlazorUserManagement` 設為啟動專案後按 F5
4. 以 `admin` / `admin123` 登入

詳細步驟見 [files/README.md](files/README.md)。

## 測試

修改程式後執行測試，確認沒有「改 A 錯 B」。

| 測試 | 內容 | 執行方式 | 需求 |
|------|------|----------|------|
| 單元測試 | 元件與規則（數字格式、民國年、日期區間、電話、上傳、快速鍵…）、表單驗證、服務層、CSV 匯入 | `dotnet test PST.sln` | 無（使用記憶體資料庫） |
| 瀏覽器端對端測試 | 以 Edge 實際操作所有畫面（登入、查詢、新增、編輯、刪除、元件展示、寄件登記、匯入），並查詢資料庫確認結果 | `.\tests\run-e2e.ps1` | SQL Server LocalDB、sqlcmd、Edge |

- **單元測試**：幾秒內完成，每次修改都可以執行；推送到 GitHub 時也會自動執行（`.github/workflows/unit-tests.yml`）。
- **瀏覽器端對端測試**：約 2 分鐘，會重設 **LocalDB** 中的測試資料庫（不影響遠端資料庫），建議在大改版或發布前執行。截圖存於 `%TEMP%\pst-e2e-shots`。
- 執行測試前請先停止 Visual Studio 中執行的系統，避免檔案被鎖定而無法建置。
