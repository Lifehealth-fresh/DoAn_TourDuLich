/* Payment gateway metadata. Run after 001-014 on existing databases. */
IF COL_LENGTH(N'dbo.ThanhToan', N'Gateway') IS NULL
    ALTER TABLE dbo.ThanhToan ADD Gateway nvarchar(20) NULL;

IF COL_LENGTH(N'dbo.ThanhToan', N'GatewayTxnId') IS NULL
    ALTER TABLE dbo.ThanhToan ADD GatewayTxnId nvarchar(100) NULL;

IF COL_LENGTH(N'dbo.ThanhToan', N'GatewayOrderId') IS NULL
    ALTER TABLE dbo.ThanhToan ADD GatewayOrderId nvarchar(100) NULL;

IF COL_LENGTH(N'dbo.ThanhToan', N'PayUrl') IS NULL
    ALTER TABLE dbo.ThanhToan ADD PayUrl nvarchar(2000) NULL;

IF COL_LENGTH(N'dbo.ThanhToan', N'PaidAt') IS NULL
    ALTER TABLE dbo.ThanhToan ADD PaidAt datetime2(7) NULL;
GO

DECLARE @HadPaymentMethodCheck bit = CASE WHEN EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.ThanhToan')
      AND definition LIKE N'%PhuongThuc%'
) THEN 1 ELSE 0 END;

DECLARE @DropChecks nvarchar(max) = N'';
SELECT @DropChecks += N'ALTER TABLE dbo.ThanhToan DROP CONSTRAINT '
    + QUOTENAME(name) + N';'
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID(N'dbo.ThanhToan')
  AND definition LIKE N'%PhuongThuc%';

IF @DropChecks <> N''
    EXEC sys.sp_executesql @DropChecks;

IF @HadPaymentMethodCheck = 1
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.check_constraints
       WHERE parent_object_id = OBJECT_ID(N'dbo.ThanhToan')
         AND name = N'CK_ThanhToan_PhuongThuc'
   )
    ALTER TABLE dbo.ThanhToan ADD CONSTRAINT CK_ThanhToan_PhuongThuc
        CHECK (PhuongThuc IS NULL OR RTRIM(PhuongThuc) IN
            (N'TienMat', N'ChuyenKhoan', N'VNPay', N'MoMo'));
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ThanhToan')
      AND name = N'UX_ThanhToan_Gateway_GatewayTxnId'
)
    CREATE UNIQUE INDEX UX_ThanhToan_Gateway_GatewayTxnId
        ON dbo.ThanhToan(Gateway, GatewayTxnId)
        WHERE Gateway IS NOT NULL AND GatewayTxnId IS NOT NULL;
GO
