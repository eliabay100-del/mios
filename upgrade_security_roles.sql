USE [Inventory];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[mn].[Role]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[Role]
    (
        RoleID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RoleName NVARCHAR(50) NOT NULL UNIQUE,
        Description NVARCHAR(250) NULL
    );
END;
GO

IF OBJECT_ID(N'[mn].[Permission]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[Permission]
    (
        PermissionID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PermissionCode NVARCHAR(80) NOT NULL UNIQUE,
        Description NVARCHAR(250) NULL
    );
END;
GO

IF OBJECT_ID(N'[mn].[UserRole]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[UserRole]
    (
        UserID INT NOT NULL,
        RoleID INT NOT NULL,
        AssignedAt DATETIME2 NOT NULL CONSTRAINT DF_UserRole_AssignedAt DEFAULT SYSDATETIME(),
        PRIMARY KEY (UserID, RoleID),
        CONSTRAINT FK_UserRole_User FOREIGN KEY (UserID) REFERENCES [mn].[UUser](UserID),
        CONSTRAINT FK_UserRole_Role FOREIGN KEY (RoleID) REFERENCES [mn].[Role](RoleID)
    );
END;
GO

IF OBJECT_ID(N'[mn].[RolePermission]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[RolePermission]
    (
        RoleID INT NOT NULL,
        PermissionID INT NOT NULL,
        PRIMARY KEY (RoleID, PermissionID),
        CONSTRAINT FK_RolePermission_Role FOREIGN KEY (RoleID) REFERENCES [mn].[Role](RoleID),
        CONSTRAINT FK_RolePermission_Permission FOREIGN KEY (PermissionID) REFERENCES [mn].[Permission](PermissionID)
    );
END;
GO

IF OBJECT_ID(N'[mn].[AuditLog]', N'U') IS NULL
BEGIN
    CREATE TABLE [mn].[AuditLog]
    (
        AuditLogID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserID INT NULL,
        ActionCode NVARCHAR(80) NOT NULL,
        EntityType NVARCHAR(80) NULL,
        EntityID INT NULL,
        Details NVARCHAR(1000) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_AuditLog_CreatedAt DEFAULT SYSDATETIME(),
        CONSTRAINT FK_AuditLog_User FOREIGN KEY (UserID) REFERENCES [mn].[UUser](UserID)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM [mn].[Role] WHERE RoleName = N'Admin')
    INSERT INTO [mn].[Role] (RoleName, Description) VALUES (N'Admin', N'Full system access');
IF NOT EXISTS (SELECT 1 FROM [mn].[Role] WHERE RoleName = N'Manager')
    INSERT INTO [mn].[Role] (RoleName, Description) VALUES (N'Manager', N'Inventory, sales, purchasing, and reports');
IF NOT EXISTS (SELECT 1 FROM [mn].[Role] WHERE RoleName = N'Storekeeper')
    INSERT INTO [mn].[Role] (RoleName, Description) VALUES (N'Storekeeper', N'Receive, issue, and count inventory');
IF NOT EXISTS (SELECT 1 FROM [mn].[Role] WHERE RoleName = N'Accountant')
    INSERT INTO [mn].[Role] (RoleName, Description) VALUES (N'Accountant', N'Payments, journals, and financial reports');

IF NOT EXISTS (SELECT 1 FROM [mn].[Permission] WHERE PermissionCode = N'INVENTORY_RECEIVE')
    INSERT INTO [mn].[Permission] (PermissionCode, Description) VALUES (N'INVENTORY_RECEIVE', N'Receive inventory');
IF NOT EXISTS (SELECT 1 FROM [mn].[Permission] WHERE PermissionCode = N'INVENTORY_ISSUE')
    INSERT INTO [mn].[Permission] (PermissionCode, Description) VALUES (N'INVENTORY_ISSUE', N'Issue inventory');
IF NOT EXISTS (SELECT 1 FROM [mn].[Permission] WHERE PermissionCode = N'INVENTORY_ADJUST')
    INSERT INTO [mn].[Permission] (PermissionCode, Description) VALUES (N'INVENTORY_ADJUST', N'Adjust inventory');
IF NOT EXISTS (SELECT 1 FROM [mn].[Permission] WHERE PermissionCode = N'PURCHASE_MANAGE')
    INSERT INTO [mn].[Permission] (PermissionCode, Description) VALUES (N'PURCHASE_MANAGE', N'Manage purchases');
IF NOT EXISTS (SELECT 1 FROM [mn].[Permission] WHERE PermissionCode = N'SALES_MANAGE')
    INSERT INTO [mn].[Permission] (PermissionCode, Description) VALUES (N'SALES_MANAGE', N'Manage sales');
IF NOT EXISTS (SELECT 1 FROM [mn].[Permission] WHERE PermissionCode = N'PAYMENT_MANAGE')
    INSERT INTO [mn].[Permission] (PermissionCode, Description) VALUES (N'PAYMENT_MANAGE', N'Record payments');
IF NOT EXISTS (SELECT 1 FROM [mn].[Permission] WHERE PermissionCode = N'ACCOUNTING_VIEW')
    INSERT INTO [mn].[Permission] (PermissionCode, Description) VALUES (N'ACCOUNTING_VIEW', N'View accounting records');
IF NOT EXISTS (SELECT 1 FROM [mn].[Permission] WHERE PermissionCode = N'USER_MANAGE')
    INSERT INTO [mn].[Permission] (PermissionCode, Description) VALUES (N'USER_MANAGE', N'Manage users and roles');
GO

-- Admin receives every permission.
INSERT INTO [mn].[RolePermission] (RoleID, PermissionID)
SELECT r.RoleID, p.PermissionID
FROM [mn].[Role] r CROSS JOIN [mn].[Permission] p
WHERE r.RoleName = N'Admin'
  AND NOT EXISTS (SELECT 1 FROM [mn].[RolePermission] rp WHERE rp.RoleID = r.RoleID AND rp.PermissionID = p.PermissionID);

-- Sensible operational defaults for the other roles.
INSERT INTO [mn].[RolePermission] (RoleID, PermissionID)
SELECT r.RoleID, p.PermissionID
FROM [mn].[Role] r JOIN [mn].[Permission] p ON p.PermissionCode IN (N'INVENTORY_RECEIVE', N'INVENTORY_ISSUE', N'INVENTORY_ADJUST')
WHERE r.RoleName = N'Storekeeper'
  AND NOT EXISTS (SELECT 1 FROM [mn].[RolePermission] rp WHERE rp.RoleID = r.RoleID AND rp.PermissionID = p.PermissionID);

INSERT INTO [mn].[RolePermission] (RoleID, PermissionID)
SELECT r.RoleID, p.PermissionID
FROM [mn].[Role] r JOIN [mn].[Permission] p ON p.PermissionCode IN (N'PURCHASE_MANAGE', N'SALES_MANAGE', N'ACCOUNTING_VIEW')
WHERE r.RoleName = N'Manager'
  AND NOT EXISTS (SELECT 1 FROM [mn].[RolePermission] rp WHERE rp.RoleID = r.RoleID AND rp.PermissionID = p.PermissionID);

INSERT INTO [mn].[RolePermission] (RoleID, PermissionID)
SELECT r.RoleID, p.PermissionID
FROM [mn].[Role] r JOIN [mn].[Permission] p ON p.PermissionCode IN (N'PAYMENT_MANAGE', N'ACCOUNTING_VIEW')
WHERE r.RoleName = N'Accountant'
  AND NOT EXISTS (SELECT 1 FROM [mn].[RolePermission] rp WHERE rp.RoleID = r.RoleID AND rp.PermissionID = p.PermissionID);
GO

-- Existing users are assigned Admin once, so the migration does not lock anyone out.
INSERT INTO [mn].[UserRole] (UserID, RoleID)
SELECT u.UserID, r.RoleID
FROM [mn].[UUser] u CROSS JOIN [mn].[Role] r
WHERE r.RoleName = N'Admin'
  AND NOT EXISTS (SELECT 1 FROM [mn].[UserRole] ur WHERE ur.UserID = u.UserID);
GO
