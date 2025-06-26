-- sp_ManageUsers
CREATE PROCEDURE [dbo].[sp_ManageUsers]
    @Action NVARCHAR(50),
    @UserId NVARCHAR(450) = NULL,
    @UserName NVARCHAR(256) = NULL,
    @Email NVARCHAR(256) = NULL,
    @RoleName NVARCHAR(256) = NULL
AS
BEGIN
    IF @Action = 'List'
    BEGIN
        SELECT u.Id, u.UserName, u.Email, r.Name AS Role
        FROM AspNetUsers u
        LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
        LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
    END
IF @Action = 'AssignRole'
BEGIN
    DECLARE @RoleId NVARCHAR(450)
    SELECT @RoleId = Id FROM AspNetRoles WHERE Name = @RoleName

    -- Remove all existing roles for the user
    DELETE FROM AspNetUserRoles WHERE UserId = @UserId

    -- Assign the new role
    IF @RoleId IS NOT NULL
    BEGIN
        INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)
    END
END

    IF @Action = 'RemoveRole'
    BEGIN
        DELETE FROM AspNetUserRoles WHERE UserId = @UserId
    END
END;

-- sp_ManageModuleAccess
CREATE PROCEDURE [dbo].[sp_ManageModuleAccess]
    @Action NVARCHAR(50),
    @RoleName NVARCHAR(256) = NULL,
    @ModuleName NVARCHAR(100) = NULL,
    @CanAccess BIT = NULL
AS
BEGIN
    IF @Action = 'Upsert'
    BEGIN
        IF NOT EXISTS (
            SELECT 1 
            FROM ModuleAccess 
            WHERE 
                (
                    (@RoleName IS NULL AND RoleName IS NULL) 
                    OR RoleName = @RoleName
                )
                AND
                (
                    (@ModuleName IS NULL AND ModuleName IS NULL) 
                    OR ModuleName = @ModuleName
                )
        )
        BEGIN
            INSERT INTO ModuleAccess (RoleName, ModuleName, CanAccess)
            VALUES (@RoleName, @ModuleName, @CanAccess)
        END
        ELSE
        BEGIN
            UPDATE ModuleAccess
            SET CanAccess = @CanAccess
            WHERE 
                (
                    (@RoleName IS NULL AND RoleName IS NULL) 
                    OR RoleName = @RoleName
                )
                AND
                (
                    (@ModuleName IS NULL AND ModuleName IS NULL) 
                    OR ModuleName = @ModuleName
                )
        END
    END

IF @Action = 'List'
BEGIN
    SELECT RoleName, ModuleName, CanAccess
    FROM ModuleAccess
    WHERE (@RoleName IS NULL OR RoleName = @RoleName)
      AND (@ModuleName IS NULL OR ModuleName = @ModuleName)
END

END

-- sp_ManageChartofAccounts
CREATE OR ALTER PROCEDURE [dbo].[sp_ManageChartofAccounts]
    @Action NVARCHAR(50),
    @Id INT = NULL,
    @AccountName NVARCHAR(100) = NULL,
    @AccountType NVARCHAR(50) = NULL,
    @ParentId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @Action = 'Create'
    BEGIN
        INSERT INTO ChartOfAccounts (AccountName, AccountType, ParentId)
        VALUES (@AccountName, @AccountType, @ParentId)

        SELECT SCOPE_IDENTITY() AS Id
    END

    ELSE IF @Action = 'Update'
    BEGIN
        -- Prevent circular reference: Ensure new ParentId is not a descendant of current Id
        IF @ParentId IS NOT NULL
        BEGIN
            -- Store descendants in a table variable
            DECLARE @Descendants TABLE (Id INT)

            ;WITH RecursiveAccounts AS (
                SELECT Id
                FROM ChartOfAccounts
                WHERE ParentId = @Id

                UNION ALL

                SELECT ca.Id
                FROM ChartOfAccounts ca
                INNER JOIN RecursiveAccounts ra ON ca.ParentId = ra.Id
            )
            INSERT INTO @Descendants (Id)
            SELECT Id FROM RecursiveAccounts

            -- Check if new ParentId is a descendant
            IF EXISTS (SELECT 1 FROM @Descendants WHERE Id = @ParentId)
            BEGIN
                RAISERROR('Invalid update: Circular reference detected.', 16, 1)
                RETURN
            END
        END

        UPDATE ChartOfAccounts
        SET AccountName = @AccountName,
            AccountType = @AccountType,
            ParentId = @ParentId
        WHERE Id = @Id
    END

    ELSE IF @Action = 'Delete'
    BEGIN
        ;WITH RecursiveAccounts AS (
            SELECT Id
            FROM ChartOfAccounts
            WHERE Id = @Id

            UNION ALL

            SELECT ca.Id
            FROM ChartOfAccounts ca
            INNER JOIN RecursiveAccounts ra ON ca.ParentId = ra.Id
        )
        DELETE FROM ChartOfAccounts
        WHERE Id IN (SELECT Id FROM RecursiveAccounts)
    END

    ELSE IF @Action = 'List'
    BEGIN
        SELECT Id, AccountName, AccountType, ParentId
        FROM ChartOfAccounts
    END
END

-- sp_SaveVoucher
CREATE PROCEDURE sp_SaveVoucher
    @VoucherType NVARCHAR(50),
    @VoucherDate DATE,
    @ReferenceNo NVARCHAR(50),
    @Details VoucherDetailsType READONLY
AS
BEGIN
    BEGIN TRANSACTION
    DECLARE @VoucherId INT
    INSERT INTO Vouchers (VoucherType, VoucherDate, ReferenceNo)
    VALUES (@VoucherType, @VoucherDate, @ReferenceNo)
    SET @VoucherId = SCOPE_IDENTITY()

    INSERT INTO VoucherDetails (VoucherId, AccountId, Debit, Credit)
    SELECT @VoucherId, AccountId, Debit, Credit
    FROM @Details
    COMMIT
END;

-- sp_GetVouchers
CREATE PROCEDURE sp_GetVouchers
AS
BEGIN
    SELECT Id, VoucherType, VoucherDate, ReferenceNo
    FROM Vouchers
END;