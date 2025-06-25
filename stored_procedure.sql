-- sp_ManageUsers
CREATE PROCEDURE sp_ManageUsers
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
        IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @UserId AND RoleId = @RoleId)
        BEGIN
            INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)
        END
    END
    IF @Action = 'RemoveRole'
    BEGIN
        DECLARE @RoleId2 NVARCHAR(450)
        SELECT @RoleId2 = Id FROM AspNetRoles WHERE Name = @RoleName
        DELETE FROM AspNetUserRoles WHERE UserId = @UserId AND RoleId = @RoleId2
    END
END;

-- sp_ManageModuleAccess
ALTER PROCEDURE [dbo].[sp_ManageModuleAccess]
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
    END
END
