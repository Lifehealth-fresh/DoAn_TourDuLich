using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.Infrastructure;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AigoiY> AigoiYs { get; set; }

    public virtual DbSet<AnhTour> AnhTours { get; set; }

    public virtual DbSet<DanhGiaHdv> DanhGiaHdvs { get; set; }

    public virtual DbSet<DanhGiaSanPhamDoiTac> DanhGiaSanPhamDoiTacs { get; set; }

    public virtual DbSet<DanhGiaTour> DanhGiaTours { get; set; }

    public virtual DbSet<MediaDanhGiaTour> MediaDanhGiaTours { get; set; }
    public virtual DbSet<MediaDanhGiaHdv> MediaDanhGiaHdvs { get; set; }
    public virtual DbSet<MediaDanhGiaSanPham> MediaDanhGiaSanPhams { get; set; }

    public virtual DbSet<DanhSachYeuThich> DanhSachYeuThiches { get; set; }

    public virtual DbSet<DatDichVu> DatDichVus { get; set; }

    public virtual DbSet<DatDichVuKhuyenMai> DatDichVuKhuyenMais { get; set; }

    public virtual DbSet<DiemThamQuan> DiemThamQuans { get; set; }

    public virtual DbSet<DieuKienKm> DieuKienKms { get; set; }

    public virtual DbSet<DoiTac> DoiTacs { get; set; }

    public virtual DbSet<GiayTo> GiayTos { get; set; }

    public virtual DbSet<HanhViKhachHang> HanhViKhachHangs { get; set; }

    public virtual DbSet<HopDong> HopDongs { get; set; }

    public virtual DbSet<HuongDanVien> HuongDanViens { get; set; }

    public virtual DbSet<KhachHang> KhachHangs { get; set; }

    public virtual DbSet<KhuVuc> KhuVucs { get; set; }

    public virtual DbSet<KhuyenMai> KhuyenMais { get; set; }

    public virtual DbSet<KmTour> KmTours { get; set; }

    public virtual DbSet<LichDanTour> LichDanTours { get; set; }

    public virtual DbSet<LichKhoiHanh> LichKhoiHanhs { get; set; }

    public virtual DbSet<LichTrinh> LichTrinhs { get; set; }

    public virtual DbSet<LichTrinhDeXuat> LichTrinhDeXuats { get; set; }
    public virtual DbSet<LichTrinhDeXuatChiTiet> LichTrinhDeXuatChiTiets { get; set; }

    public virtual DbSet<JobRunLog> JobRunLogs { get; set; }

    public virtual DbSet<NguoiSuDung> NguoiSuDungs { get; set; }

    public virtual DbSet<NhomKhuyenMai> NhomKhuyenMais { get; set; }

    public virtual DbSet<Quyen> Quyens { get; set; }

    public virtual DbSet<SanPhamDoiTac> SanPhamDoiTacs { get; set; }

    public virtual DbSet<ThanhToan> ThanhToans { get; set; }

    public virtual DbSet<ThongBao> ThongBaos { get; set; }

    public virtual DbSet<Tour> Tours { get; set; }

    public virtual DbSet<VaiTro> VaiTros { get; set; }

    public virtual DbSet<YeuCauThietKe> YeuCauThietKes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobRunLog>(entity =>
        {
            entity.HasKey(e => e.MaJobRun);
            entity.ToTable("JobRunLog");
            entity.Property(e => e.TenJob).HasMaxLength(100);
            entity.Property(e => e.ThoiDiemBatDau).HasColumnType("datetime2");
            entity.Property(e => e.ThoiDiemKetThuc).HasColumnType("datetime2");
            entity.Property(e => e.TrangThai).HasMaxLength(20);
            entity.Property(e => e.Loi).HasMaxLength(2000);
        });

        modelBuilder.Entity<AigoiY>(entity =>
        {
            entity.HasKey(e => e.MaRecommodation).HasName("PK__AIGoiY__A3B0666F69D2BCA6");

            entity.ToTable("AIGoiY");

            entity.Property(e => e.MaRecommodation)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.LyDo).HasMaxLength(200);
            entity.Property(e => e.MaTour)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.MaUser)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.NgayGoiY).HasColumnType("datetime");

            entity.HasOne(d => d.MaTourNavigation).WithMany(p => p.AigoiYs)
                .HasForeignKey(d => d.MaTour)
                .HasConstraintName("FK_AIGoiY_Tour");

            entity.HasOne(d => d.MaUserNavigation).WithMany(p => p.AigoiYs)
                .HasForeignKey(d => d.MaUser)
                .HasConstraintName("FK_AIGoiY_NguoiSuDung");
        });

        modelBuilder.Entity<AnhTour>(entity =>
        {
            entity.HasKey(e => e.MaAnhTour).HasName("PK__AnhTour__37570F04DE1E5659");

            entity.ToTable("AnhTour");

            entity.Property(e => e.MaAnhTour)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(300)
                .HasColumnName("ImageURL");
            entity.Property(e => e.Url).HasMaxLength(300).HasColumnName("Url");
            entity.Property(e => e.LoaiMedia).HasMaxLength(10).IsRequired();
            entity.Property(e => e.CloudPublicId).HasMaxLength(255);
            entity.Property(e => e.CloudResourceType).HasMaxLength(10);
            entity.Property(e => e.MaTour)
                .HasMaxLength(20)
                .IsFixedLength();

            entity.HasOne(d => d.MaTourNavigation).WithMany(p => p.AnhTours)
                .HasForeignKey(d => d.MaTour)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AnhTour_Tour");
        });

        modelBuilder.Entity<DanhGiaHdv>(entity =>
        {
            entity.HasKey(e => e.MaDanhGiaHdv).HasName("PK__DanhGiaH__B5C25953990767E5");

            entity.ToTable("DanhGiaHDV");

            entity.Property(e => e.MaDanhGiaHdv)
                .HasMaxLength(20)
                .IsFixedLength()
                .HasColumnName("MaDanhGiaHDV");
            entity.Property(e => e.MaHdv)
                .HasMaxLength(20)
                .IsFixedLength()
                .HasColumnName("MaHDV");
            entity.Property(e => e.MaUser)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.ThoiGian).HasColumnType("datetime");

            entity.HasOne(d => d.MaHdvNavigation).WithMany(p => p.DanhGiaHdvs)
                .HasForeignKey(d => d.MaHdv)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DanhGiaHDV_HDV");

            entity.HasOne(d => d.MaUserNavigation).WithMany(p => p.DanhGiaHdvs)
                .HasForeignKey(d => d.MaUser)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DanhGiaHDV_NguoiSuDung");
        });

        modelBuilder.Entity<DanhGiaSanPhamDoiTac>(entity =>
        {
            entity.HasKey(e => e.MaDanhGia).HasName("PK__DanhGiaS__AA9515BFC96F0F08");

            entity.ToTable("DanhGiaSanPhamDoiTac");

            entity.Property(e => e.MaDanhGia)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.MaSanPham)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.MaUser)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.ThoiGian).HasColumnType("datetime");

            entity.HasOne(d => d.MaSanPhamNavigation).WithMany(p => p.DanhGiaSanPhamDoiTacs)
                .HasForeignKey(d => d.MaSanPham)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DanhGiaSPDT_SanPham");

            entity.HasOne(d => d.MaUserNavigation).WithMany(p => p.DanhGiaSanPhamDoiTacs)
                .HasForeignKey(d => d.MaUser)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DanhGiaSPDT_NguoiSuDung");
        });

        modelBuilder.Entity<DanhGiaTour>(entity =>
        {
            entity.HasKey(e => e.MaDanhGiaTour).HasName("PK__DanhGiaT__9528E52EEFECFAEA");

            entity.ToTable("DanhGiaTour");

            entity.Property(e => e.MaDanhGiaTour)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.MaTour)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.MaUser)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.ThoiGian).HasColumnType("datetime");

            entity.HasOne(d => d.MaTourNavigation).WithMany(p => p.DanhGiaTours)
                .HasForeignKey(d => d.MaTour)
                .HasConstraintName("FK_DanhGiaTour_Tour");

            entity.HasOne(d => d.MaUserNavigation).WithMany(p => p.DanhGiaTours)
                .HasForeignKey(d => d.MaUser)
                .HasConstraintName("FK_DanhGiaTour_NguoiSuDung");
        });

        modelBuilder.Entity<MediaDanhGiaTour>(entity =>
        {
            entity.HasKey(e => e.MaMedia).HasName("PK_MediaDanhGiaTour");
            entity.ToTable("MediaDanhGiaTour");
            entity.Property(e => e.MaMedia).HasMaxLength(20).IsFixedLength();
            entity.Property(e => e.MaDanhGiaTour).HasMaxLength(20).IsFixedLength();
            entity.Property(e => e.LoaiMedia).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Url).HasMaxLength(300).IsRequired();
            entity.HasOne(e => e.MaDanhGiaTourNavigation)
                .WithMany(e => e.MediaDanhGiaTours)
                .HasForeignKey(e => e.MaDanhGiaTour)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_MediaDanhGiaTour_DanhGiaTour");
        });

        modelBuilder.Entity<MediaDanhGiaHdv>(entity =>
        {
            entity.HasKey(e => e.MaMedia).HasName("PK_MediaDanhGiaHdv");
            entity.ToTable("MediaDanhGiaHdv");
            entity.Property(e => e.MaMedia).HasMaxLength(20).IsFixedLength();
            entity.Property(e => e.MaDanhGiaHdv).HasMaxLength(20).IsFixedLength();
            entity.Property(e => e.LoaiMedia).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Url).HasMaxLength(300).IsRequired();
            entity.HasOne(e => e.MaDanhGiaHdvNavigation)
                .WithMany(e => e.MediaDanhGiaHdvs)
                .HasForeignKey(e => e.MaDanhGiaHdv)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_MediaDanhGiaHdv_DanhGiaHdv");
        });

        modelBuilder.Entity<MediaDanhGiaSanPham>(entity =>
        {
            entity.HasKey(e => e.MaMedia).HasName("PK_MediaDanhGiaSanPham");
            entity.ToTable("MediaDanhGiaSanPham");
            entity.Property(e => e.MaMedia).HasMaxLength(20).IsFixedLength();
            entity.Property(e => e.MaDanhGia).HasMaxLength(20).IsFixedLength();
            entity.Property(e => e.LoaiMedia).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Url).HasMaxLength(300).IsRequired();
            entity.HasOne(e => e.MaDanhGiaNavigation)
                .WithMany(e => e.MediaDanhGiaSanPhams)
                .HasForeignKey(e => e.MaDanhGia)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_MediaDanhGiaSanPham_DanhGia");
        });

        modelBuilder.Entity<DanhSachYeuThich>(entity =>
        {
            entity.HasKey(e => e.MaWish).HasName("PK__DanhSach__9591F60866139E0E");

            entity.ToTable("DanhSachYeuThich");

            entity.Property(e => e.MaWish)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.MaTour)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.MaUser)
                .HasMaxLength(20)
                .IsFixedLength();

            entity.HasOne(d => d.MaTourNavigation).WithMany(p => p.DanhSachYeuThiches)
                .HasForeignKey(d => d.MaTour)
                .HasConstraintName("FK_YeuThich_Tour");

            entity.HasOne(d => d.MaUserNavigation).WithMany(p => p.DanhSachYeuThiches)
                .HasForeignKey(d => d.MaUser)
                .HasConstraintName("FK_YeuThich_NguoiSuDung");
        });

        modelBuilder.Entity<DatDichVu>(entity =>
        {
            entity.HasKey(e => e.MaBooking).HasName("PK__DatDichV__745D1755A75F6F73");

            entity.ToTable("DatDichVu");

            entity.Property(e => e.MaBooking)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.MaKhoiHanh)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.MaKhachHang)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.MaTour)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.MaUser)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.SlnguoiLon).HasColumnName("SLNguoiLon");
            entity.Property(e => e.SltreEm).HasColumnName("SLTreEm");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.TyLePhatHuy);
            entity.Property(e => e.SoTienPhatHuy);

            entity.HasOne(d => d.MaKhoiHanhNavigation).WithMany(p => p.DatDichVus)
                .HasForeignKey(d => d.MaKhoiHanh)
                .HasConstraintName("FK_DatDichVu_KhoiHanh");

            entity.HasOne(d => d.MaKhachHangNavigation).WithMany()
                .HasForeignKey(d => d.MaKhachHang)
                .HasConstraintName("FK_DatDichVu_KhachHang");

            entity.HasOne(d => d.MaTourNavigation).WithMany(p => p.DatDichVus)
                .HasForeignKey(d => d.MaTour)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DatDichVu_Tour");

            entity.HasOne(d => d.MaUserNavigation).WithMany(p => p.DatDichVus)
                .HasForeignKey(d => d.MaUser)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DatDichVu_NguoiSuDung");
        });

        modelBuilder.Entity<DatDichVuKhuyenMai>(entity =>
        {
            entity.HasKey(e => e.Stt).HasName("PK__DatDichV__CA1EB69094A6CEF2");

            entity.ToTable("DatDichVu_KhuyenMai");

            entity.Property(e => e.Stt).HasColumnName("STT");
            entity.Property(e => e.MaBooking)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.MaKhuyenMai)
                .HasMaxLength(20)
                .IsFixedLength();

            entity.HasOne(d => d.MaBookingNavigation).WithMany(p => p.DatDichVuKhuyenMais)
                .HasForeignKey(d => d.MaBooking)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DDVKM_DatDichVu");

            entity.HasOne(d => d.MaKhuyenMaiNavigation).WithMany(p => p.DatDichVuKhuyenMais)
                .HasForeignKey(d => d.MaKhuyenMai)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DDVKM_KhuyenMai");
        });

        modelBuilder.Entity<DiemThamQuan>(entity =>
        {
            entity.HasKey(e => e.MaDthamQuan).HasName("PK__DiemTham__D63288D94C43D593");

            entity.ToTable("DiemThamQuan");

            entity.Property(e => e.MaDthamQuan)
                .HasMaxLength(20)
                .IsFixedLength()
                .HasColumnName("MaDThamQuan");
            entity.Property(e => e.DiaChi)
                .HasMaxLength(100)
                .IsFixedLength();
            entity.Property(e => e.KinhDo).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.MaKhuVuc)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.TenDiaDanh).HasMaxLength(100);
            entity.Property(e => e.ViDo).HasColumnType("decimal(9, 6)");

            entity.HasOne(d => d.MaKhuVucNavigation).WithMany(p => p.DiemThamQuans)
                .HasForeignKey(d => d.MaKhuVuc)
                .HasConstraintName("FK_DiemThamQuan_KhuVuc");
        });

        modelBuilder.Entity<DieuKienKm>(entity =>
        {
            entity.HasKey(e => e.MaDk).HasName("PK__DieuKien__2725866CB08CF5E2");

            entity.ToTable("DieuKienKM");

            entity.Property(e => e.MaDk)
                .HasMaxLength(20)
                .IsFixedLength()
                .HasColumnName("MaDK");
            entity.Property(e => e.MaKhuyenMai)
                .HasMaxLength(20)
                .IsFixedLength();

            entity.HasOne(d => d.MaKhuyenMaiNavigation).WithMany(p => p.DieuKienKms)
                .HasForeignKey(d => d.MaKhuyenMai)
                .HasConstraintName("FK_DieuKienKM_KhuyenMai");
        });

        modelBuilder.Entity<DoiTac>(entity =>
        {
            entity.HasKey(e => e.MaDoiTac).HasName("PK__DoiTac__5F76BF346A5AA8AD");

            entity.ToTable("DoiTac");

            entity.Property(e => e.MaDoiTac)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.LoaiDoiTac)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.MaKhuVuc)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.NguoiLienHe).HasMaxLength(50);
            entity.Property(e => e.PhanTramHoaHong).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.TenDoiTac).HasMaxLength(150);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsFixedLength();

            entity.HasOne(d => d.MaKhuVucNavigation).WithMany(p => p.DoiTacs)
                .HasForeignKey(d => d.MaKhuVuc)
                .HasConstraintName("FK_DoiTac_KhuVuc");
        });

        modelBuilder.Entity<GiayTo>(entity =>
        {
            entity.HasKey(e => e.MaGiayTo).HasName("PK__GiayTo__D6796CCA857969B2");

            entity.ToTable("GiayTo");

            entity.Property(e => e.MaGiayTo)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.LoaiGiayTo).HasMaxLength(50);
            entity.Property(e => e.MaKhachHang)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.NoiCap).HasMaxLength(50);
            entity.Property(e => e.SoTrenGiayTo).HasMaxLength(50);

            entity.HasOne(d => d.MaKhachHangNavigation).WithMany(p => p.GiayTos)
                .HasForeignKey(d => d.MaKhachHang)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GiayTo_KhachHang");
        });

        modelBuilder.Entity<HanhViKhachHang>(entity =>
        {
            entity.HasKey(e => e.MaHanhDong).HasName("PK__HanhViKh__F0C73927D41AB929");

            entity.ToTable("HanhViKhachHang");

            entity.Property(e => e.MaHanhDong)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.HanhDong).HasMaxLength(20);
            entity.Property(e => e.MaTour)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.MaUser)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.ThoiGian).HasColumnType("datetime");

            entity.HasOne(d => d.MaTourNavigation).WithMany(p => p.HanhViKhachHangs)
                .HasForeignKey(d => d.MaTour)
                .HasConstraintName("FK_HanhVi_Tour");

            entity.HasOne(d => d.MaUserNavigation).WithMany(p => p.HanhViKhachHangs)
                .HasForeignKey(d => d.MaUser)
                .HasConstraintName("FK_HanhVi_NguoiSuDung");
        });

        modelBuilder.Entity<HopDong>(entity =>
        {
            entity.HasKey(e => e.MaHopDong).HasName("PK__HopDong__36DD43427321DC20");

            entity.ToTable("HopDong");

            entity.Property(e => e.MaHopDong)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.FileHopDongUrl)
                .HasMaxLength(300)
                .HasColumnName("FileHopDongURL");
            entity.Property(e => e.MaBooking)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.NguoiDaiDien)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.SoHopDong).HasMaxLength(50);
            entity.Property(e => e.HoTenKhach).HasMaxLength(70);
            entity.Property(e => e.LoaiGiayTo).HasMaxLength(50);
            entity.Property(e => e.SoGiayTo).HasMaxLength(50);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsFixedLength();

            entity.HasOne(d => d.MaBookingNavigation).WithMany(p => p.HopDongs)
                .HasForeignKey(d => d.MaBooking)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HopDong_DatDichVu");

            entity.HasOne(d => d.NguoiDaiDienNavigation).WithMany(p => p.HopDongs)
                .HasForeignKey(d => d.NguoiDaiDien)
                .HasConstraintName("FK_HopDong_NguoiSuDung");
        });

        modelBuilder.Entity<HuongDanVien>(entity =>
        {
            entity.HasKey(e => e.MaHuongDanVien).HasName("PK__HuongDan__D3D8C377AB40C50B");

            entity.ToTable("HuongDanVien");

            entity.Property(e => e.MaHuongDanVien)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.Cccd)
                .HasMaxLength(30)
                .HasColumnName("CCCD");
            entity.Property(e => e.Email).HasMaxLength(50);
            entity.Property(e => e.HoTen).HasMaxLength(50);
            entity.Property(e => e.QueQuan).HasMaxLength(100);
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(20)
                .IsFixedLength();
        });

        modelBuilder.Entity<KhachHang>(entity =>
        {
            entity.HasKey(e => e.MaKhachHang).HasName("PK__KhachHan__88D2F0E5AD907E53");

            entity.ToTable("KhachHang");

            entity.Property(e => e.MaKhachHang)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.DanhXung)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.GioiTinh)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.Ho).HasMaxLength(20);
            entity.Property(e => e.HoGiayTo).HasMaxLength(20);
            entity.Property(e => e.MaUser)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.QuocTich).HasMaxLength(50);
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(15)
                .IsFixedLength();
            entity.Property(e => e.Ten).HasMaxLength(50);
            entity.Property(e => e.TenGiayTo).HasMaxLength(50);

            entity.HasOne(d => d.MaUserNavigation).WithMany(p => p.KhachHangs)
                .HasForeignKey(d => d.MaUser)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_KhachHang_NguoiSuDung");
        });

        modelBuilder.Entity<KhuVuc>(entity =>
        {
            entity.HasKey(e => e.MaKhuVuc).HasName("PK__KhuVuc__0676EB833B7D4842");

            entity.ToTable("KhuVuc");

            entity.Property(e => e.MaKhuVuc)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.KinhDo).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.MuiGio).HasMaxLength(30);
            entity.Property(e => e.QuocGia)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.TenKhuVuc).HasMaxLength(50);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.ViDo).HasColumnType("decimal(9, 6)");
        });

        modelBuilder.Entity<KhuyenMai>(entity =>
        {
            entity.HasKey(e => e.MaKm).HasName("PK__KhuyenMa__2725CF15521E44AF");

            entity.ToTable("KhuyenMai");

            entity.Property(e => e.MaKm)
                .HasMaxLength(20)
                .IsFixedLength()
                .HasColumnName("MaKM");
            entity.Property(e => e.DonVi)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.MaCode)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.MaNhomKm)
                .HasMaxLength(20)
                .IsFixedLength()
                .HasColumnName("MaNhomKM");
            entity.Property(e => e.NgayBd)
                .HasColumnType("datetime")
                .HasColumnName("NgayBD");
            entity.Property(e => e.NgayKt)
                .HasColumnType("datetime")
                .HasColumnName("NgayKT");
            entity.Property(e => e.TenKm)
                .HasMaxLength(50)
                .HasColumnName("TenKM");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsFixedLength();

            entity.HasOne(d => d.MaNhomKmNavigation).WithMany(p => p.KhuyenMais)
                .HasForeignKey(d => d.MaNhomKm)
                .HasConstraintName("FK_KhuyenMai_NhomKM");
        });

        modelBuilder.Entity<KmTour>(entity =>
        {
            entity.HasKey(e => e.Stt).HasName("PK__KM_Tour__CA1EB690F45625CD");

            entity.ToTable("KM_Tour");

            entity.Property(e => e.Stt).HasColumnName("STT");
            entity.Property(e => e.MaKhuyenMai)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.MaTour)
                .HasMaxLength(20)
                .IsFixedLength();

            entity.HasOne(d => d.MaKhuyenMaiNavigation).WithMany(p => p.KmTours)
                .HasForeignKey(d => d.MaKhuyenMai)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_KMTour_KhuyenMai");

            entity.HasOne(d => d.MaTourNavigation).WithMany(p => p.KmTours)
                .HasForeignKey(d => d.MaTour)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_KMTour_Tour");
        });

        modelBuilder.Entity<LichDanTour>(entity =>
        {
            entity.HasKey(e => e.MaLichDanTour).HasName("PK__LichDanT__D22F397F992D2E75");

            entity.ToTable("LichDanTour");

            entity.Property(e => e.MaLichDanTour)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.MaHdv)
                .HasMaxLength(20)
                .IsFixedLength()
                .HasColumnName("MaHDV");
            entity.Property(e => e.MaKhoiHanh)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.MaTour)
                .HasMaxLength(20)
                .IsFixedLength();

            entity.HasOne(d => d.MaHdvNavigation).WithMany(p => p.LichDanTours)
                .HasForeignKey(d => d.MaHdv)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LichDanTour_HDV");

            entity.HasOne(d => d.MaKhoiHanhNavigation).WithMany(p => p.LichDanTours)
                .HasForeignKey(d => d.MaKhoiHanh)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LichDanTour_KhoiHanh");

            entity.HasOne(d => d.MaTourNavigation).WithMany(p => p.LichDanTours)
                .HasForeignKey(d => d.MaTour)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LichDanTour_Tour");
        });

        modelBuilder.Entity<LichKhoiHanh>(entity =>
        {
            entity.HasKey(e => e.MaKhoiHanh).HasName("PK__LichKhoi__B8672C9C9AA07494");

            entity.ToTable("LichKhoiHanh");

            entity.Property(e => e.MaKhoiHanh)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.DiaDiem).HasMaxLength(100);
            entity.Property(e => e.MaTour)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.NgayKetThuc).HasColumnType("datetime");
            entity.Property(e => e.NgayKhoiHanh).HasColumnType("datetime");

            entity.HasOne(d => d.MaTourNavigation).WithMany(p => p.LichKhoiHanhs)
                .HasForeignKey(d => d.MaTour)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LichKhoiHanh_Tour");
        });

        modelBuilder.Entity<LichTrinh>(entity =>
        {
            entity.HasKey(e => e.MaLichTrinh).HasName("PK__LichTrin__32E7201D586622F5");

            entity.ToTable("LichTrinh");

            entity.Property(e => e.MaLichTrinh)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.MaDthamQuan)
                .HasMaxLength(20)
                .IsFixedLength()
                .HasColumnName("MaDThamQuan");
            entity.Property(e => e.MaSanPham)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.MaTour)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.ThanhTien).HasComputedColumnSql("(isnull([SoLuong],(0))*isnull([DonGia],(0)))", true);
            entity.Property(e => e.ThoiGianDuKien).HasColumnType("datetime");

            entity.HasOne(d => d.MaDthamQuanNavigation).WithMany(p => p.LichTrinhs)
                .HasForeignKey(d => d.MaDthamQuan)
                .HasConstraintName("FK_LichTrinh_DiemThamQuan");

            entity.HasOne(d => d.MaSanPhamNavigation).WithMany(p => p.LichTrinhs)
                .HasForeignKey(d => d.MaSanPham)
                .HasConstraintName("FK_LichTrinh_SanPham");

            entity.HasOne(d => d.MaTourNavigation).WithMany(p => p.LichTrinhs)
                .HasForeignKey(d => d.MaTour)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LichTrinh_Tour");
        });

        modelBuilder.Entity<LichTrinhDeXuat>(entity =>
        {
            entity.HasKey(e => e.MaDeXuat).HasName("PK_LichTrinhDeXuat");
            entity.ToTable("LichTrinhDeXuat");
            entity.Property(e => e.MaDeXuat).HasMaxLength(20).IsFixedLength();
            entity.Property(e => e.MaYeuCau).HasMaxLength(20).IsFixedLength();
            entity.Property(e => e.TenPhuongAn).HasMaxLength(100).IsRequired();
            entity.Property(e => e.GhiChu).HasMaxLength(500);
            entity.Property(e => e.TrangThai).HasMaxLength(20).IsFixedLength().IsRequired();
            entity.Property(e => e.NgayTao).HasColumnType("datetime2");
            entity.HasOne(e => e.MaYeuCauNavigation).WithMany(e => e.LichTrinhDeXuats)
                .HasForeignKey(e => e.MaYeuCau).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_LichTrinhDeXuat_YeuCau");
        });

        modelBuilder.Entity<LichTrinhDeXuatChiTiet>(entity =>
        {
            entity.HasKey(e => e.MaChiTiet).HasName("PK_LichTrinhDeXuatChiTiet");
            entity.ToTable("LichTrinhDeXuatChiTiet");
            entity.Property(e => e.MaChiTiet).HasMaxLength(20).IsFixedLength();
            entity.Property(e => e.MaDeXuat).HasMaxLength(20).IsFixedLength();
            entity.Property(e => e.MaDthamQuan).HasMaxLength(20).IsFixedLength().HasColumnName("MaDThamQuan");
            entity.Property(e => e.MaSanPham).HasMaxLength(20).IsFixedLength();
            entity.Property(e => e.Mota).HasMaxLength(500);
            entity.HasOne(e => e.MaDeXuatNavigation).WithMany(e => e.ChiTiets)
                .HasForeignKey(e => e.MaDeXuat).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_LichTrinhDeXuatChiTiet_DeXuat");
            entity.HasOne(e => e.MaDthamQuanNavigation).WithMany()
                .HasForeignKey(e => e.MaDthamQuan).HasConstraintName("FK_LichTrinhDeXuatChiTiet_Diem");
            entity.HasOne(e => e.MaSanPhamNavigation).WithMany()
                .HasForeignKey(e => e.MaSanPham).HasConstraintName("FK_LichTrinhDeXuatChiTiet_SanPham");
        });

        modelBuilder.Entity<NguoiSuDung>(entity =>
        {
            entity.HasKey(e => e.MaUser).HasName("PK__NguoiSuD__55DAC4B7DBE7C8AF");

            entity.ToTable("NguoiSuDung");

            entity.HasIndex(e => e.SoDienThoai, "UQ_NguoiSuDung_SoDienThoai").IsUnique();

            entity.Property(e => e.MaUser)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.MatKhau).HasMaxLength(200);
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(20)
                .IsFixedLength();

            entity.HasOne(d => d.MaVaiTroNavigation).WithMany(p => p.NguoiSuDungs)
                .HasForeignKey(d => d.MaVaiTro)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NguoiSuDung_VaiTro");
        });

        modelBuilder.Entity<NhomKhuyenMai>(entity =>
        {
            entity.HasKey(e => e.MaNhomKm).HasName("PK__NhomKhuy__5A1F657AAC1B49E4");

            entity.ToTable("NhomKhuyenMai");

            entity.Property(e => e.MaNhomKm)
                .HasMaxLength(20)
                .IsFixedLength()
                .HasColumnName("MaNhomKM");
            entity.Property(e => e.TenNhomKm)
                .HasMaxLength(50)
                .HasColumnName("TenNhomKM");
        });

        modelBuilder.Entity<Quyen>(entity =>
        {
            entity.HasKey(e => e.MaQuyen).HasName("PK__Quyen__1D4B7ED47DADFDD6");

            entity.ToTable("Quyen");

            entity.Property(e => e.MaQuyen).ValueGeneratedNever();
            entity.Property(e => e.Mota).HasMaxLength(100);
            entity.Property(e => e.TenQuyen).HasMaxLength(50);

            entity.HasOne(d => d.MaVaiTroNavigation).WithMany(p => p.Quyens)
                .HasForeignKey(d => d.MaVaiTro)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Quyen_VaiTro");
        });

        modelBuilder.Entity<SanPhamDoiTac>(entity =>
        {
            entity.HasKey(e => e.MaSanPham).HasName("PK__SanPhamD__FAC7442DB9C6A617");

            entity.ToTable("SanPhamDoiTac");

            entity.Property(e => e.MaSanPham)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.DonViTinh).HasMaxLength(30);
            entity.Property(e => e.MaDoiTac)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.MaDthamQuan)
                .HasMaxLength(20)
                .IsFixedLength()
                .HasColumnName("MaDThamQuan");
            entity.Property(e => e.Mota).HasMaxLength(300);
            entity.Property(e => e.TenSanPham).HasMaxLength(150);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsFixedLength();

            entity.HasOne(d => d.MaDoiTacNavigation).WithMany(p => p.SanPhamDoiTacs)
                .HasForeignKey(d => d.MaDoiTac)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SanPhamDoiTac_DoiTac");

            entity.HasOne(d => d.MaDthamQuanNavigation).WithMany(p => p.SanPhamDoiTacs)
                .HasForeignKey(d => d.MaDthamQuan)
                .HasConstraintName("FK_SanPhamDoiTac_DiemThamQuan");
        });

        modelBuilder.Entity<ThanhToan>(entity =>
        {
            entity.HasKey(e => e.MaTt).HasName("PK__ThanhToa__2725007977DBE2B1");

            entity.ToTable("ThanhToan");

            entity.Property(e => e.MaTt)
                .HasMaxLength(20)
                .IsFixedLength()
                .HasColumnName("MaTT");
            entity.Property(e => e.LoaiThanhToan)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.IdempotencyKey).HasMaxLength(100);
            entity.Property(e => e.MaBooking)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.NgayTt)
                .HasColumnType("datetime")
                .HasColumnName("NgayTT");
            entity.Property(e => e.PhuongThuc).HasMaxLength(30);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsFixedLength();

            entity.HasOne(d => d.MaBookingNavigation).WithMany(p => p.ThanhToans)
                .HasForeignKey(d => d.MaBooking)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ThanhToan_DatDichVu");
        });

        modelBuilder.Entity<ThongBao>(entity =>
        {
            entity.HasKey(e => e.MaThongBao).HasName("PK__ThongBao__04DEB54EF2F13D18");

            entity.ToTable("ThongBao");

            entity.Property(e => e.MaThongBao)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.MaUser)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.TieuDe).HasMaxLength(50);

            entity.HasOne(d => d.MaUserNavigation).WithMany(p => p.ThongBaos)
                .HasForeignKey(d => d.MaUser)
                .HasConstraintName("FK_ThongBao_NguoiSuDung");
        });

        modelBuilder.Entity<Tour>(entity =>
        {
            entity.HasKey(e => e.MaTour).HasName("PK__Tour__4E5557DE4C8B2BC3");

            entity.ToTable("Tour");

            entity.Property(e => e.MaTour)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.LoaiTour)
                .HasMaxLength(20)
                .HasDefaultValue("Chuan")
                .IsFixedLength();
            entity.Property(e => e.SlhuongDanVien).HasColumnName("SLHuongDanVien");
            entity.Property(e => e.Slkhach).HasColumnName("SLKhach");
            entity.Property(e => e.TenTour).HasMaxLength(150);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsFixedLength();
        });

        modelBuilder.Entity<VaiTro>(entity =>
        {
            entity.HasKey(e => e.MaVaiTro).HasName("PK__VaiTro__C24C41CFBBF0F214");

            entity.ToTable("VaiTro");

            entity.Property(e => e.MaVaiTro).ValueGeneratedNever();
            entity.Property(e => e.Mota).HasMaxLength(100);
            entity.Property(e => e.TenVaiTro).HasMaxLength(50);
        });

        modelBuilder.Entity<YeuCauThietKe>(entity =>
        {
            entity.HasKey(e => e.MaYeuCau).HasName("PK__YeuCauTh__CFA5DF4E47C84949");

            entity.ToTable("YeuCauThietKe");

            entity.Property(e => e.MaYeuCau)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.DiemDenMongMuon).HasMaxLength(200);
            entity.Property(e => e.LyDoTuChoiGoiY).HasMaxLength(200);
            entity.Property(e => e.LyDoTuChoiBoiSale).HasColumnType("nvarchar(max)");
            entity.Property(e => e.MaGoiYthamKhao)
                .HasMaxLength(20)
                .IsFixedLength()
                .HasColumnName("MaGoiYThamKhao");
            entity.Property(e => e.MaTourTao)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.MaUser)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.NgayGui).HasColumnType("datetime");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsFixedLength();

            entity.HasOne(d => d.MaGoiYthamKhaoNavigation).WithMany(p => p.YeuCauThietKes)
                .HasForeignKey(d => d.MaGoiYthamKhao)
                .HasConstraintName("FK_YeuCauThietKe_AIGoiY");

            entity.HasOne(d => d.MaTourTaoNavigation).WithMany(p => p.YeuCauThietKes)
                .HasForeignKey(d => d.MaTourTao)
                .HasConstraintName("FK_YeuCauThietKe_Tour");

            entity.HasOne(d => d.MaUserNavigation).WithMany(p => p.YeuCauThietKes)
                .HasForeignKey(d => d.MaUser)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_YeuCauThietKe_NguoiSuDung");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
