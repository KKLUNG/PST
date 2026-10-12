-- ============================================
-- Blazor User Management System
-- SQL Server 資料庫建立腳本
-- 適用版本: SQL Server 2016 及以上
-- ============================================

-- 1. 建立資料庫
IF EXISTS (SELECT * FROM sys.databases WHERE name = 'BlazorUserManagement')
BEGIN
    ALTER DATABASE BlazorUserManagement SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE BlazorUserManagement;
END

CREATE DATABASE BlazorUserManagement;
GO

USE BlazorUserManagement;
GO

-- 2. 建立使用者資料表
CREATE TABLE [dbo].[Users] (
    [UserId] INT PRIMARY KEY IDENTITY(1,1) NOT NULL,
    [Username] NVARCHAR(50) NOT NULL UNIQUE,
    [Email] NVARCHAR(100) NOT NULL UNIQUE,
    [PasswordHash] NVARCHAR(255) NOT NULL,
    [FullName] NVARCHAR(100),
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
    [UpdatedDate] DATETIME,
    [LastLoginDate] DATETIME
);

-- 3. 建立索引以提升查詢效能
CREATE INDEX [IX_Users_Username] ON [dbo].[Users]([Username]);
CREATE INDEX [IX_Users_Email] ON [dbo].[Users]([Email]);
CREATE INDEX [IX_Users_IsActive] ON [dbo].[Users]([IsActive]);

-- 4. 建立檢查約束
ALTER TABLE [dbo].[Users] 
ADD CONSTRAINT [CK_Email_Format] 
CHECK (Email LIKE '%@%.%');

-- 5. 插入測試資料
-- 備註: 以下密碼均為 'admin123' 使用 BCrypt 加密的結果
INSERT INTO [dbo].[Users] 
    ([Username], [Email], [PasswordHash], [FullName], [IsActive], [CreatedDate], [LastLoginDate])
VALUES 
    ('admin', 'admin@company.com', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcg7b3XeKeUxWdeS86E36XQuvKm', '系統管理員', 1, GETDATE(), GETDATE()),
    ('user1', 'user1@company.com', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcg7b3XeKeUxWdeS86E36XQuvKm', '李使用者', 1, GETDATE(), NULL),
    ('user2', 'user2@company.com', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcg7b3XeKeUxWdeS86E36XQuvKm', '王使用者', 1, GETDATE(), NULL);

-- 6. 建立檢視表 - 使用者統計
CREATE VIEW [dbo].[vw_UserStatistics] AS
SELECT 
    COUNT(*) AS [TotalUsers],
    SUM(CASE WHEN [IsActive] = 1 THEN 1 ELSE 0 END) AS [ActiveUsers],
    SUM(CASE WHEN [IsActive] = 0 THEN 1 ELSE 0 END) AS [InactiveUsers],
    COUNT(DISTINCT CONVERT(DATE, [CreatedDate])) AS [DaysWithNewUsers],
    MAX([CreatedDate]) AS [LatestUserCreated]
FROM [dbo].[Users];

-- 7. 建立預存程序 - 取得使用者總數
CREATE PROCEDURE [sp_GetUserCount]
    @ActiveOnly BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @ActiveOnly = 1
        SELECT COUNT(*) AS UserCount FROM [dbo].[Users] WHERE [IsActive] = 1;
    ELSE
        SELECT COUNT(*) AS UserCount FROM [dbo].[Users];
END;

-- 8. 建立預存程序 - 更新使用者最後登入時間
CREATE PROCEDURE [sp_UpdateLastLogin]
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE [dbo].[Users]
    SET [LastLoginDate] = GETDATE()
    WHERE [UserId] = @UserId;
END;

-- 9. 設定權限 (選用)
-- 建立應用程式專用帳號並設定權限
/*
CREATE LOGIN [BlazorAppUser] WITH PASSWORD = 'Strong#Password123';
CREATE USER [BlazorAppUser] FOR LOGIN [BlazorAppUser];
GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[Users] TO [BlazorAppUser];
GRANT EXECUTE ON [sp_GetUserCount] TO [BlazorAppUser];
GRANT EXECUTE ON [sp_UpdateLastLogin] TO [BlazorAppUser];
*/

-- 10. 驗證資料
PRINT '========== 資料庫建立完成 =========='
PRINT ''
PRINT '表格列表:'
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo'
PRINT ''
PRINT '測試資料:'
SELECT [UserId], [Username], [Email], [FullName], [IsActive], [CreatedDate] FROM [dbo].[Users]
PRINT ''
PRINT '資料庫統計:'
SELECT * FROM [dbo].[vw_UserStatistics]
PRINT ''
PRINT '========== 初始化完成 =========='
