/* Chạy thủ công trên database đã có schema 001-005. Không tự chạy migration. */
ALTER TABLE dbo.DatDichVu ADD MaKhachHang nchar(20) NULL;
GO

ALTER TABLE dbo.DatDichVu
ADD CONSTRAINT FK_DatDichVu_KhachHang FOREIGN KEY (MaKhachHang)
REFERENCES dbo.KhachHang (MaKhachHang);
GO

ALTER TABLE dbo.HopDong ADD HoTenKhach nvarchar(70) NULL;
GO

ALTER TABLE dbo.HopDong ADD LoaiGiayTo nvarchar(50) NULL;
GO

ALTER TABLE dbo.HopDong ADD SoGiayTo nvarchar(50) NULL;
GO

ALTER TABLE dbo.DatDichVu ADD TyLePhatHuy INT NULL;
GO

ALTER TABLE dbo.DatDichVu ADD SoTienPhatHuy INT NULL;
GO
