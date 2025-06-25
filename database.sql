-- Create Tables
CREATE TABLE ModuleAccess (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(256),
    ModuleName NVARCHAR(100),
    CanAccess BIT
);

CREATE TABLE ChartOfAccounts (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AccountName NVARCHAR(100),
    AccountType NVARCHAR(50),
    ParentId INT NULL,
    FOREIGN KEY (ParentId) REFERENCES ChartOfAccounts(Id)
);

CREATE TABLE Vouchers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    VoucherType NVARCHAR(50),
    VoucherDate DATE,
    ReferenceNo NVARCHAR(50)
);

CREATE TABLE VoucherDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    VoucherId INT,
    AccountId INT,
    Debit DECIMAL(18,2),
    Credit DECIMAL(18,2),
    FOREIGN KEY (VoucherId) REFERENCES Vouchers(Id),
    FOREIGN KEY (AccountId) REFERENCES ChartOfAccounts(Id)
);

-- Create User-Defined Table Type for Voucher Details
CREATE TYPE VoucherDetailsType AS TABLE (
    AccountId INT,
    Debit DECIMAL(18,2),
    Credit DECIMAL(18,2)
);