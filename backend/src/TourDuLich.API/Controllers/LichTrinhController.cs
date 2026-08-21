using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourDuLich.API.DTOs;
using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LichTrinhController : ControllerBase
{
    private readonly AppDbContext _context;

    public LichTrinhController(AppDbContext context)
    {
        _context = context;
    }

    // GHI CHÚ: Endpoint công khai theo yêu cầu nghiệp vụ — khách xem được lịch
    // trình bất kỳ tour nào (kể cả tour đang được thiết kế riêng cho người khác)
    // mà không cần đăng nhập. Nếu sau này cần giới hạn quyền riêng tư cho tour
    // đang ở trạng thái "Nhap" (đang thiết kế dở), cân nhắc bổ sung điều kiện tại đây.

    // GET /api/LichTrinh/tour/{maTour}
    [HttpGet("tour/{maTour}")]
    public async Task<ActionResult> GetByTour(string maTour)
    {
        var maTourDb = FixedLengthHelper.PadTo20(maTour);

        var tour = await _context.Tours
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.MaTour == maTourDb);

        if (tour is null)
        {
            return NotFound(new
            {
                message = $"Không tìm thấy tour '{maTour}'."
            });
        }

        var items = await _context.LichTrinhs
            .AsNoTracking()
            .Where(item => item.MaTour == maTourDb)
            .OrderBy(item => item.NgayThu)
            .ThenBy(item => item.ThuTuTrongNgay)
            .Select(item => new
            {
                maLichTrinh = FixedLengthHelper.TrimSafe(item.MaLichTrinh),
                maTour = FixedLengthHelper.TrimSafe(item.MaTour),
                ngayThu = item.NgayThu,
                thuTuTrongNgay = item.ThuTuTrongNgay,
                maDthamQuan = FixedLengthHelper.TrimSafe(item.MaDthamQuan),
                tenDiaDanh = item.MaDthamQuanNavigation!.TenDiaDanh,
                maSanPham = FixedLengthHelper.TrimSafe(item.MaSanPham),
                tenSanPham = item.MaSanPhamNavigation!.TenSanPham,
                soLuong = item.SoLuong,
                donGia = item.DonGia,
                thanhTien = item.ThanhTien,
                thoiGianDuKien = item.ThoiGianDuKien,
                mota = item.Mota
            })
            .ToListAsync();

        var tongGiaHienTai = items.Sum(item => item.thanhTien ?? 0);

        return Ok(new
        {
            maTour = FixedLengthHelper.TrimSafe(tour.MaTour),
            tenTour = tour.TenTour,
            tongGiaHienTai,
            lichTrinh = items
        });
    }

    // POST /api/LichTrinh
    [HttpPost]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<ActionResult> Create(LichTrinhCreateDto request)
    {
        var maTourDb = FixedLengthHelper.PadTo20(request.MaTour);

        if (string.IsNullOrWhiteSpace(request.MaDthamQuan) &&
            string.IsNullOrWhiteSpace(request.MaSanPham))
        {
            return BadRequest(new
            {
                message = "Phải có ít nhất một trong MaDthamQuan hoặc MaSanPham."
            });
        }

        if (request.SoLuong <= 0)
        {
            return BadRequest(new
            {
                message = "Số lượng phải lớn hơn 0."
            });
        }

        var coQuyen = await UserCanManageTourAsync(maTourDb);

        if (!coQuyen)
        {
            return Forbid();
        }

        var tour = await _context.Tours
            .FirstOrDefaultAsync(item => item.MaTour == maTourDb);

        if (tour is null)
        {
            return NotFound(new
            {
                message = $"Không tìm thấy tour '{request.MaTour}'."
            });
        }

        if (FixedLengthHelper.TrimSafe(tour.TrangThai) != "Nhap")
        {
            return BadRequest(new
            {
                message = "Chỉ tour đang ở trạng thái Nhap mới có thể chỉnh sửa lịch trình."
            });
        }

        var maDthamQuanDb = await ValidateDiemThamQuanAsync(
            request.MaDthamQuan);

        if (request.MaDthamQuan is not null &&
            maDthamQuanDb is null)
        {
            return BadRequest(new
            {
                message = "Điểm tham quan không tồn tại."
            });
        }

        var productData = await GetProductDataAsync(
            request.MaSanPham);

        if (request.MaSanPham is not null &&
            productData is null)
        {
            return BadRequest(new
            {
                message = "Sản phẩm đối tác không tồn tại."
            });
        }

        var maLichTrinhDb = await GenerateMaLichTrinhAsync();

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var lichTrinh = new LichTrinh
        {
            MaLichTrinh = maLichTrinhDb,
            MaTour = maTourDb,
            NgayThu = request.NgayThu,
            ThuTuTrongNgay = request.ThuTuTrongNgay,
            MaDthamQuan = maDthamQuanDb,
            MaSanPham = productData?.MaSanPham,
            SoLuong = request.SoLuong,
            DonGia = productData?.GiaNiemYet ?? 0,
            Mota = request.Mota?.Trim()
        };

        _context.LichTrinhs.Add(lichTrinh);

        await _context.SaveChangesAsync();
        await _context.Entry(lichTrinh).ReloadAsync();

        var giaTourMoi = await RecalculateTourPriceAsync(tour);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            maLichTrinh = FixedLengthHelper.TrimSafe(lichTrinh.MaLichTrinh),
            maTour = FixedLengthHelper.TrimSafe(lichTrinh.MaTour),
            ngayThu = lichTrinh.NgayThu,
            thuTuTrongNgay = lichTrinh.ThuTuTrongNgay,
            maDthamQuan = FixedLengthHelper.TrimSafe(lichTrinh.MaDthamQuan),
            maSanPham = FixedLengthHelper.TrimSafe(lichTrinh.MaSanPham),
            soLuong = lichTrinh.SoLuong,
            donGia = lichTrinh.DonGia,
            thanhTien = lichTrinh.ThanhTien,
            mota = lichTrinh.Mota,
            giaTourMoi
        });
    }

    // PUT /api/LichTrinh/{maLichTrinh}
    [HttpPut("{maLichTrinh}")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<IActionResult> Update(
        string maLichTrinh,
        LichTrinhUpdateDto request)
    {
        if (string.IsNullOrWhiteSpace(request.MaDthamQuan) &&
            string.IsNullOrWhiteSpace(request.MaSanPham))
        {
            return BadRequest(new
            {
                message = "Phải có ít nhất một trong MaDthamQuan hoặc MaSanPham."
            });
        }

        if (request.SoLuong <= 0)
        {
            return BadRequest(new
            {
                message = "Số lượng phải lớn hơn 0."
            });
        }

        var maLichTrinhDb = FixedLengthHelper.PadTo20(maLichTrinh);

        var lichTrinh = await _context.LichTrinhs
            .FirstOrDefaultAsync(item =>
                item.MaLichTrinh == maLichTrinhDb);

        if (lichTrinh is null)
        {
            return NotFound(new
            {
                message = $"Không tìm thấy dòng lịch trình '{maLichTrinh}'."
            });
        }

        var coQuyen = await UserCanManageTourAsync(lichTrinh.MaTour);

        if (!coQuyen)
        {
            return Forbid();
        }

        var tour = await _context.Tours
            .FirstOrDefaultAsync(item =>
                item.MaTour == lichTrinh.MaTour);

        if (tour is null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy tour của lịch trình."
            });
        }

        if (FixedLengthHelper.TrimSafe(tour.TrangThai) != "Nhap")
        {
            return BadRequest(new
            {
                message = "Chỉ tour đang ở trạng thái Nhap mới có thể chỉnh sửa lịch trình."
            });
        }

        var maDthamQuanDb = await ValidateDiemThamQuanAsync(
            request.MaDthamQuan);

        if (request.MaDthamQuan is not null &&
            maDthamQuanDb is null)
        {
            return BadRequest(new
            {
                message = "Điểm tham quan không tồn tại."
            });
        }

        var productData = await GetProductDataAsync(
            request.MaSanPham);

        if (request.MaSanPham is not null &&
            productData is null)
        {
            return BadRequest(new
            {
                message = "Sản phẩm đối tác không tồn tại."
            });
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        lichTrinh.MaDthamQuan = maDthamQuanDb;
        lichTrinh.MaSanPham = productData?.MaSanPham;
        lichTrinh.SoLuong = request.SoLuong;
        lichTrinh.DonGia = productData?.GiaNiemYet ?? 0;
        lichTrinh.Mota = request.Mota?.Trim();

        await _context.SaveChangesAsync();
        await _context.Entry(lichTrinh).ReloadAsync();

        var giaTourMoi = await RecalculateTourPriceAsync(tour);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            maLichTrinh = FixedLengthHelper.TrimSafe(lichTrinh.MaLichTrinh),
            maTour = FixedLengthHelper.TrimSafe(lichTrinh.MaTour),
            maDthamQuan = FixedLengthHelper.TrimSafe(lichTrinh.MaDthamQuan),
            maSanPham = FixedLengthHelper.TrimSafe(lichTrinh.MaSanPham),
            soLuong = lichTrinh.SoLuong,
            donGia = lichTrinh.DonGia,
            thanhTien = lichTrinh.ThanhTien,
            mota = lichTrinh.Mota,
            giaTourMoi
        });
    }

    // DELETE /api/LichTrinh/{maLichTrinh}
    [HttpDelete("{maLichTrinh}")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<IActionResult> Delete(string maLichTrinh)
    {
        var maLichTrinhDb = FixedLengthHelper.PadTo20(maLichTrinh);

        var lichTrinh = await _context.LichTrinhs
            .FirstOrDefaultAsync(item =>
                item.MaLichTrinh == maLichTrinhDb);

        if (lichTrinh is null)
        {
            return NotFound(new
            {
                message = $"Không tìm thấy dòng lịch trình '{maLichTrinh}'."
            });
        }

        var coQuyen = await UserCanManageTourAsync(lichTrinh.MaTour);

        if (!coQuyen)
        {
            return Forbid();
        }

        var tour = await _context.Tours
            .FirstOrDefaultAsync(item =>
                item.MaTour == lichTrinh.MaTour);

        if (tour is null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy tour của lịch trình."
            });
        }

        if (FixedLengthHelper.TrimSafe(tour.TrangThai) != "Nhap")
        {
            return BadRequest(new
            {
                message = "Chỉ tour đang ở trạng thái Nhap mới có thể xóa lịch trình."
            });
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        _context.LichTrinhs.Remove(lichTrinh);

        await _context.SaveChangesAsync();

        var giaTourMoi = await RecalculateTourPriceAsync(tour);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            message = "Đã xóa dòng lịch trình.",
            giaTourMoi
        });
    }

    private async Task<bool> UserCanAccessTourAsync(string maTourDb)
    {
        if (IsAdminOrSale())
        {
            return true;
        }

        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return false;
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);

        return await _context.YeuCauThietKes
            .AnyAsync(item =>
                item.MaUser == maUserDb &&
                item.MaTourTao == maTourDb);
    }

    private async Task<bool> UserCanManageTourAsync(string maTourDb)
    {
        return await UserCanAccessTourAsync(maTourDb);
    }

    private bool IsAdminOrSale()
    {
        var maVaiTro = User.FindFirst("MaVaiTro")?.Value;

        // TODO: chuyển sang policy-based authorization khi hệ thống có role claim chuẩn
        return maVaiTro == "2" || maVaiTro == "3";
    }

    private string? GetCurrentMaUser()
    {
        return User.FindFirst("MaUser")?.Value;
    }

    private async Task<string?> ValidateDiemThamQuanAsync(
        string? maDthamQuan)
    {
        if (string.IsNullOrWhiteSpace(maDthamQuan))
        {
            return null;
        }

        var maDthamQuanDb =
            FixedLengthHelper.PadTo20(maDthamQuan);

        var exists = await _context.DiemThamQuans
            .AnyAsync(item =>
                item.MaDthamQuan == maDthamQuanDb);

        return exists
            ? maDthamQuanDb
            : null;
    }

    private async Task<ProductData?> GetProductDataAsync(
        string? maSanPham)
    {
        if (string.IsNullOrWhiteSpace(maSanPham))
        {
            return null;
        }

        var maSanPhamDb =
            FixedLengthHelper.PadTo20(maSanPham);

        return await _context.SanPhamDoiTacs
            .Where(item =>
                item.MaSanPham == maSanPhamDb)
            .Select(item => new ProductData(
                item.MaSanPham,
                item.GiaNiemYet))
            .FirstOrDefaultAsync();
    }

    private async Task<string> GenerateMaLichTrinhAsync()
    {
        string maLichTrinh;
        string maLichTrinhDb;

        do
        {
            maLichTrinh =
                $"LT{Guid.NewGuid():N}"[..20]
                .ToUpperInvariant();

            maLichTrinhDb =
                FixedLengthHelper.PadTo20(maLichTrinh);
        }
        while (await _context.LichTrinhs
            .AnyAsync(item =>
                item.MaLichTrinh == maLichTrinhDb));

        return maLichTrinhDb;
    }

    private async Task<int> RecalculateTourPriceAsync(Tour tour)
    {
        var tongGia = await _context.LichTrinhs
            .Where(item =>
                item.MaTour == tour.MaTour)
            .SumAsync(item =>
                (int?)item.ThanhTien) ?? 0;

        // TODO: cộng thêm markup % khi tính GiaTour cuối cùng,
        // hiện tại GiaTour = tổng giá gốc từ đối tác
        tour.GiaTour = tongGia;

        return tongGia;
    }

    private sealed record ProductData(
        string MaSanPham,
        int GiaNiemYet);
}
