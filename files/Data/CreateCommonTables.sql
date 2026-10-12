-- ============================================
-- 共用資料表：參數檔、地址 (郵遞區號)
-- 供底層元件 BaseParameterSelect、BaseAddress 使用
-- 可重複執行：資料表不存在才建立，資料不存在才新增 (不會刪除既有資料)
-- 執行方式：sqlcmd -S 伺服器 -d BlazorUserManagement -f 65001 -i Data\CreateCommonTables.sql
-- ============================================

-- 1. 參數檔
--    SetItem：參數群組 (例如 UNIT)；SetType：代碼；SetValue：顯示名稱
IF OBJECT_ID(N'dbo.Parameters', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Parameters] (
        [SetItem]   NVARCHAR(30)  NOT NULL,
        [SetType]   NVARCHAR(30)  NOT NULL,
        [SetValue]  NVARCHAR(100) NOT NULL,
        [SortOrder] INT           NOT NULL DEFAULT 0,
        [Memo]      NVARCHAR(200) NULL,
        [IsDeleted] BIT           NOT NULL DEFAULT 0,
        CONSTRAINT [PK_Parameters] PRIMARY KEY ([SetItem], [SetType])
    );
END
GO

-- 範例參數 (UNIT 的 D 為已刪除，示範不會出現在選單中)
INSERT INTO [dbo].[Parameters] ([SetItem], [SetType], [SetValue], [SortOrder], [IsDeleted])
SELECT v.SetItem, v.SetType, v.SetValue, v.SortOrder, v.IsDeleted
FROM (VALUES
    (N'UNIT',     N'A', N'台北郵局',   1, 0),
    (N'UNIT',     N'B', N'台中郵局',   2, 0),
    (N'UNIT',     N'C', N'高雄郵局',   3, 0),
    (N'UNIT',     N'D', N'花蓮郵局',   4, 1),
    (N'PAY_TYPE', N'1', N'現金',       1, 0),
    (N'PAY_TYPE', N'2', N'信用卡',     2, 0),
    (N'PAY_TYPE', N'3', N'轉帳',       3, 0),
    (N'PAY_TYPE', N'4', N'郵政劃撥',   4, 0)
) AS v (SetItem, SetType, SetValue, SortOrder, IsDeleted)
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[Parameters] p WHERE p.SetItem = v.SetItem AND p.SetType = v.SetType
);
GO

-- 2. 地址：縣市、鄉鎮市區、3 碼郵遞區號
IF OBJECT_ID(N'dbo.AddressAreas', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AddressAreas] (
        [County]    NVARCHAR(10) NOT NULL,
        [District]  NVARCHAR(10) NOT NULL,
        [ZipCode]   VARCHAR(3)   NOT NULL,
        [SortOrder] INT          NOT NULL DEFAULT 0,
        CONSTRAINT [PK_AddressAreas] PRIMARY KEY ([County], [District])
    );
END
GO

-- ※ 以下為範例資料 (僅臺北市、新北市、臺中市、高雄市部分行政區)
--   正式使用請匯入中華郵政公布的完整郵遞區號資料
INSERT INTO [dbo].[AddressAreas] ([County], [District], [ZipCode], [SortOrder])
SELECT v.County, v.District, v.ZipCode, v.SortOrder
FROM (VALUES
    (N'臺北市', N'中正區', '100', 101), (N'臺北市', N'大同區', '103', 102), (N'臺北市', N'中山區', '104', 103),
    (N'臺北市', N'松山區', '105', 104), (N'臺北市', N'大安區', '106', 105), (N'臺北市', N'萬華區', '108', 106),
    (N'臺北市', N'信義區', '110', 107), (N'臺北市', N'士林區', '111', 108), (N'臺北市', N'北投區', '112', 109),
    (N'臺北市', N'內湖區', '114', 110), (N'臺北市', N'南港區', '115', 111), (N'臺北市', N'文山區', '116', 112),
    (N'新北市', N'板橋區', '220', 201), (N'新北市', N'汐止區', '221', 202), (N'新北市', N'新店區', '231', 203),
    (N'新北市', N'永和區', '234', 204), (N'新北市', N'中和區', '235', 205), (N'新北市', N'土城區', '236', 206),
    (N'新北市', N'樹林區', '238', 207), (N'新北市', N'三重區', '241', 208), (N'新北市', N'新莊區', '242', 209),
    (N'新北市', N'蘆洲區', '247', 210),
    (N'臺中市', N'中區',   '400', 301), (N'臺中市', N'東區',   '401', 302), (N'臺中市', N'南區',   '402', 303),
    (N'臺中市', N'西區',   '403', 304), (N'臺中市', N'北區',   '404', 305), (N'臺中市', N'北屯區', '406', 306),
    (N'臺中市', N'西屯區', '407', 307), (N'臺中市', N'南屯區', '408', 308),
    (N'高雄市', N'新興區', '800', 401), (N'高雄市', N'前金區', '801', 402), (N'高雄市', N'苓雅區', '802', 403),
    (N'高雄市', N'鹽埕區', '803', 404), (N'高雄市', N'鼓山區', '804', 405), (N'高雄市', N'旗津區', '805', 406),
    (N'高雄市', N'前鎮區', '806', 407), (N'高雄市', N'三民區', '807', 408), (N'高雄市', N'楠梓區', '811', 409),
    (N'高雄市', N'小港區', '812', 410), (N'高雄市', N'左營區', '813', 411)
) AS v (County, District, ZipCode, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[AddressAreas] a WHERE a.County = v.County AND a.District = v.District
);
GO

PRINT '========== 共用資料表建立完成 =========='
