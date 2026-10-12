<#
.SYNOPSIS
    執行瀏覽器端對端測試 (E2E)

.DESCRIPTION
    1. 重設 LocalDB 測試資料庫 (執行 CreateDatabase.sql、CreateCommonTables.sql)
    2. 建置並啟動系統 (連線字串以環境變數指向 LocalDB，不會使用 appsettings.json 的資料庫)
    3. 以 Playwright 操作 Edge 執行所有畫面測試
    4. 關閉系統，回傳結果 (0 = 全部通過)

    需求：SQL Server LocalDB、sqlcmd、Microsoft Edge
    ※ 會刪除並重建 LocalDB 中的 BlazorUserManagement 資料庫 (不影響遠端資料庫)

.EXAMPLE
    .\tests\run-e2e.ps1
    .\tests\run-e2e.ps1 -ShotDir C:\temp\shots
#>
param(
    # 截圖存放資料夾
    [string]$ShotDir = (Join-Path $env:TEMP "pst-e2e-shots"),
    # 系統測試用的網址
    [string]$Url = "http://localhost:5199"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$app = Join-Path $root "files"
$e2e = Join-Path $PSScriptRoot "BlazorUserManagement.E2ETests"
$out = Join-Path $env:TEMP "pst-e2e-build"
$localDb = "(localdb)\MSSQLLocalDB"
$connection = "Server=$localDb;Database=BlazorUserManagement;Integrated Security=true;TrustServerCertificate=true;"

Write-Host "== 1. 重設 LocalDB 測試資料庫 ==" -ForegroundColor Cyan
sqlcmd -S $localDb -E -b -f 65001 -i (Join-Path $app "Data\CreateDatabase.sql") | Out-Null
if ($LASTEXITCODE -ne 0) { throw "CreateDatabase.sql 執行失敗" }
sqlcmd -S $localDb -E -b -f 65001 -d BlazorUserManagement -i (Join-Path $app "Data\CreateCommonTables.sql") | Out-Null
if ($LASTEXITCODE -ne 0) { throw "CreateCommonTables.sql 執行失敗" }

Write-Host "== 2. 建置 ==" -ForegroundColor Cyan
# 輸出到暫存資料夾，避免與 Visual Studio 執行中的程式搶檔案
dotnet build (Join-Path $app "BlazorUserManagement.csproj") -nologo -v q -p:OutDir="$out\app\"
if ($LASTEXITCODE -ne 0) { throw "系統建置失敗" }
dotnet build $e2e -nologo -v q -p:OutDir="$out\e2e\"
if ($LASTEXITCODE -ne 0) { throw "E2E 測試建置失敗" }

Write-Host "== 3. 啟動系統 ($Url) ==" -ForegroundColor Cyan
$env:ConnectionStrings__DefaultConnection = $connection
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = $Url
$server = Start-Process dotnet -ArgumentList "`"$out\app\BlazorUserManagement.dll`" --contentRoot `"$app`"" `
    -PassThru -WindowStyle Hidden -RedirectStandardOutput "$out\app.log" -RedirectStandardError "$out\app.err.log"

try {
    $ready = $false
    for ($i = 0; $i -lt 60; $i++) {
        try {
            Invoke-WebRequest "$Url/health" -UseBasicParsing -TimeoutSec 2 | Out-Null
            $ready = $true
            break
        } catch {
            Start-Sleep -Seconds 1
        }
    }
    if (-not $ready) { throw "系統啟動逾時，請查看 $out\app.log" }

    Write-Host "== 4. 執行瀏覽器測試 ==" -ForegroundColor Cyan
    $env:E2E_BASE_URL = $Url
    $env:E2E_DB = $connection
    & "$out\e2e\BlazorUserManagement.E2ETests.exe" $ShotDir
    $result = $LASTEXITCODE
    Write-Host "截圖：$ShotDir"
    exit $result
}
finally {
    Stop-Process -Id $server.Id -Force -ErrorAction SilentlyContinue
}
