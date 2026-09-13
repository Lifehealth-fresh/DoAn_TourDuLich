using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourDuLich.API.DTOs;
using TourDuLich.API.Authorization;
using TourDuLich.Application.Helpers;
using TourDuLich.Application.Services;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

using Microsoft.AspNetCore.Authorization;

namespace TourDuLich.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SanPhamDoiTacController : ControllerBase
{
    private readonly AppDbContext _context;

    public SanPhamDoiTacController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult> GetProducts(
        [FromQuery] string? maDoiTac = null,
        [FromQuery] string? maDthamQuan = null,
        [FromQuery] string? maKhuVuc = null,
        [FromQuery] string? loaiDoiTac = null)
    {
        var query = _context.SanPhamDoiTacs
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(maDoiTac))
        {
            var maDoiTacDb = FixedLengthHelper.PadTo20(maDoiTac);
            query = query.Where(item => item.MaDoiTac == maDoiTacDb);
        }

        if (!string.IsNullOrWhiteSpace(maDthamQuan))
        {
            var maDthamQuanDb = FixedLengthHelper.PadTo20(maDthamQuan);
            query = query.Where(item => item.MaDthamQuan == maDthamQuanDb);
        }

        if (!string.IsNullOrWhiteSpace(maKhuVuc))
        {
            var maKhuVucDb = FixedLengthHelper.PadTo20(maKhuVuc);
            query = query.Where(item => item.MaDoiTacNavigation.MaKhuVuc == maKhuVucDb);
        }

        if (!string.IsNullOrWhiteSpace(loaiDoiTac))
        {
            var loaiDb = FixedLengthHelper.PadTo20(loaiDoiTac);
            query = query.Where(item => item.MaDoiTacNavigation.LoaiDoiTac == loaiDb);
        }

        var result = await query
            .Select(item => new
            {
                maSanPham = FixedLengthHelper.TrimSafe(item.MaSanPham),
                maDoiTac = FixedLengthHelper.TrimSafe(item.MaDoiTac),
                tenDoiTac = item.MaDoiTacNavigation.TenDoiTac,
                loaiDoiTac = FixedLengthHelper.TrimSafe(item.MaDoiTacNavigation.LoaiDoiTac),
                maKhuVuc = FixedLengthHelper.TrimSafe(item.MaDoiTacNavigation.MaKhuVuc),
                tenKhuVuc = item.MaDoiTacNavigation.MaKhuVucNavigation != null
                    ? item.MaDoiTacNavigation.MaKhuVucNavigation.TenKhuVuc : null,
                maTinh = FixedLengthHelper.TrimSafe(item.MaDoiTacNavigation.MaTinh),
                tenSanPham = item.TenSanPham,
                donViTinh = item.DonViTinh,
                giaNiemYet = item.GiaNiemYet,
                maDthamQuan = FixedLengthHelper.TrimSafe(item.MaDthamQuan),
                tenDiaDanh = item.MaDthamQuanNavigation!.TenDiaDanh,
                mota = item.Mota,
                trangThai = FixedLengthHelper.TrimSafe(item.TrangThai),
                laKhachSan = item.MaDoiTacNavigation.LoaiDoiTac == FixedLengthHelper.PadTo20(HotelStayRules.LoaiLuuTru)
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("{maSanPham}")]
    [AllowAnonymous]
    public async Task<ActionResult> GetProduct(string maSanPham)
    {
        var maSanPhamDb = FixedLengthHelper.PadTo20(maSanPham);

        var product = await _context.SanPhamDoiTacs
            .AsNoTracking()
            .Where(item => item.MaSanPham == maSanPhamDb)
            .Select(item => new
            {
                maSanPham = FixedLengthHelper.TrimSafe(item.MaSanPham),
                maDoiTac = FixedLengthHelper.TrimSafe(item.MaDoiTac),
                tenDoiTac = item.MaDoiTacNavigation.TenDoiTac,
                tenSanPham = item.TenSanPham,
                donViTinh = item.DonViTinh,
                giaNiemYet = item.GiaNiemYet,
                maDthamQuan = FixedLengthHelper.TrimSafe(item.MaDthamQuan),
                tenDiaDanh = item.MaDthamQuanNavigation!.TenDiaDanh,
                mota = item.Mota,
                trangThai = FixedLengthHelper.TrimSafe(item.TrangThai)
            })
            .FirstOrDefaultAsync();

        if (product is null)
        {
            return NotFound(new
            {
                message = $"Không tìm thấy sản phẩm '{maSanPham}'."
            });
        }

        return Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = "Sale,Admin")]
    [RequirePermission(PermissionCatalog.DoiTac, PermissionCatalog.Them)]
    public async Task<ActionResult> CreateProduct(
        SanPhamDoiTacCreateDto request)
    {
        if (string.IsNullOrWhiteSpace(request.MaDoiTac) ||
            string.IsNullOrWhiteSpace(request.TenSanPham))
        {
            return BadRequest(new
            {
                message = "Mã đối tác và tên sản phẩm không được để trống."
            });
        }

        if (request.GiaNiemYet <= 0)
        {
            return BadRequest(new
            {
                message = "Giá niêm yết phải lớn hơn 0."
            });
        }

        var maSanPhamDb = string.IsNullOrWhiteSpace(request.MaSanPham)
            ? await GenerateMaSanPhamAsync()
            : FixedLengthHelper.PadTo20(request.MaSanPham);
        var maDoiTacDb = FixedLengthHelper.PadTo20(request.MaDoiTac);

        if (await _context.SanPhamDoiTacs
            .AnyAsync(item => item.MaSanPham == maSanPhamDb))
        {
            return Conflict(new { message = "Mã sản phẩm đã tồn tại." });
        }

        var partner = await _context.DoiTacs.AsNoTracking()
            .FirstOrDefaultAsync(item => item.MaDoiTac == maDoiTacDb);
        if (partner is null)
        {
            return BadRequest(new { message = "Đối tác không tồn tại." });
        }

        var donViTinh = request.DonViTinh?.Trim();
        if (HotelStayRules.IsHotelPartner(partner.LoaiDoiTac))
            donViTinh = "dem";

        string? maDthamQuanDb = null;

        if (!string.IsNullOrWhiteSpace(request.MaDthamQuan))
        {
            maDthamQuanDb = FixedLengthHelper.PadTo20(request.MaDthamQuan);

            if (!await _context.DiemThamQuans
                .AnyAsync(item => item.MaDthamQuan == maDthamQuanDb))
            {
                return BadRequest(new
                {
                    message = "Điểm tham quan không tồn tại."
                });
            }
        }

        var product = new SanPhamDoiTac
        {
            MaSanPham = maSanPhamDb,
            MaDoiTac = maDoiTacDb,
            TenSanPham = request.TenSanPham.Trim(),
            DonViTinh = donViTinh,
            GiaNiemYet = request.GiaNiemYet,
            MaDthamQuan = maDthamQuanDb,
            Mota = request.Mota?.Trim(),
            TrangThai = string.IsNullOrWhiteSpace(request.TrangThai)
                ? FixedLengthHelper.PadTo20("HoatDong")
                : FixedLengthHelper.PadTo20(request.TrangThai)
        };

        _context.SanPhamDoiTacs.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetProduct),
            new
            {
                maSanPham = FixedLengthHelper.TrimSafe(product.MaSanPham)
            },
            new
            {
                maSanPham = FixedLengthHelper.TrimSafe(product.MaSanPham),
                maDoiTac = FixedLengthHelper.TrimSafe(product.MaDoiTac),
                tenSanPham = product.TenSanPham,
                donViTinh = product.DonViTinh,
                giaNiemYet = product.GiaNiemYet,
                maDthamQuan = FixedLengthHelper.TrimSafe(product.MaDthamQuan),
                mota = product.Mota,
                trangThai = FixedLengthHelper.TrimSafe(product.TrangThai)
            });
    }

    [HttpPut("{maSanPham}")]
    [Authorize(Roles = "Sale,Admin")]
    [RequirePermission(PermissionCatalog.DoiTac, PermissionCatalog.Sua)]
    public async Task<IActionResult> UpdateProduct(
        string maSanPham,
        SanPhamDoiTacUpdateDto request)
    {
        var maSanPhamDb = FixedLengthHelper.PadTo20(maSanPham);

        var product = await _context.SanPhamDoiTacs
            .FirstOrDefaultAsync(item => item.MaSanPham == maSanPhamDb);

        if (product is null)
        {
            return NotFound(new
            {
                message = $"Không tìm thấy sản phẩm '{maSanPham}'."
            });
        }

        if (string.IsNullOrWhiteSpace(request.MaDoiTac) ||
            string.IsNullOrWhiteSpace(request.TenSanPham))
        {
            return BadRequest(new
            {
                message = "Mã đối tác và tên sản phẩm không được để trống."
            });
        }

        if (request.GiaNiemYet <= 0)
        {
            return BadRequest(new
            {
                message = "Giá niêm yết phải lớn hơn 0."
            });
        }

        var maDoiTacDb = FixedLengthHelper.PadTo20(request.MaDoiTac);

        if (!await _context.DoiTacs
            .AnyAsync(item => item.MaDoiTac == maDoiTacDb))
        {
            return BadRequest(new
            {
                message = "Đối tác không tồn tại."
            });
        }

        string? maDthamQuanDb = null;

        if (!string.IsNullOrWhiteSpace(request.MaDthamQuan))
        {
            maDthamQuanDb = FixedLengthHelper.PadTo20(request.MaDthamQuan);

            if (!await _context.DiemThamQuans
                .AnyAsync(item => item.MaDthamQuan == maDthamQuanDb))
            {
                return BadRequest(new
                {
                    message = "Điểm tham quan không tồn tại."
                });
            }
        }

        product.MaDoiTac = maDoiTacDb;
        product.TenSanPham = request.TenSanPham.Trim();
        product.DonViTinh = request.DonViTinh?.Trim();
        product.GiaNiemYet = request.GiaNiemYet;
        product.MaDthamQuan = maDthamQuanDb;
        product.Mota = request.Mota?.Trim();
        product.TrangThai = string.IsNullOrWhiteSpace(request.TrangThai)
            ? product.TrangThai
            : FixedLengthHelper.PadTo20(request.TrangThai);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{maSanPham}")]
    [Authorize(Roles = "Sale,Admin")]
    [RequirePermission(PermissionCatalog.DoiTac, PermissionCatalog.Xoa)]
    public async Task<IActionResult> DeleteProduct(string maSanPham)
    {
        var maSanPhamDb = FixedLengthHelper.PadTo20(maSanPham);

        var product = await _context.SanPhamDoiTacs
            .FirstOrDefaultAsync(item => item.MaSanPham == maSanPhamDb);

        if (product is null)
        {
            return NotFound(new
            {
                message = $"Không tìm thấy sản phẩm '{maSanPham}'."
            });
        }

        var isReferenced = await _context.LichTrinhs
            .AnyAsync(item => item.MaSanPham == product.MaSanPham);

        if (isReferenced)
        {
            return BadRequest(new
            {
                message = "Không thể xóa sản phẩm vì đang được lịch trình tham chiếu."
            });
        }

        _context.SanPhamDoiTacs.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<string> GenerateMaSanPhamAsync()
    {
        string key;
        do { key = FixedLengthHelper.PadTo20($"SP{Guid.NewGuid():N}"[..20].ToUpperInvariant()); }
        while (await _context.SanPhamDoiTacs.AnyAsync(item => item.MaSanPham == key));
        return key;
    }
}
