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
[Authorize]
public class YeuCauThietKeController : ControllerBase
{
    private readonly AppDbContext _context;

    public YeuCauThietKeController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("cua-toi")]
    public async Task<ActionResult> GetMyRequests()
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);

        var requests = await _context.YeuCauThietKes
            .AsNoTracking()
            .Where(item => item.MaUser == maUserDb)
            .OrderByDescending(item => item.NgayGui)
            .Select(item => new
            {
                maYeuCau = FixedLengthHelper.TrimSafe(item.MaYeuCau),
                diemDenMongMuon = item.DiemDenMongMuon,
                ngayDuKienDi = item.NgayDuKienDi,
                soNgay = item.SoNgay,
                soNguoiLon = item.SoNguoiLon,
                soTreEm = item.SoTreEm,
                nganSachDuKien = item.NganSachDuKien,
                soThichGhiChu = item.SoThichGhiChu,
                maGoiYThamKhao = FixedLengthHelper.TrimSafe(item.MaGoiYthamKhao),
                lyDoTuChoiGoiY = item.LyDoTuChoiGoiY,
                trangThai = FixedLengthHelper.TrimSafe(item.TrangThai),
                ngayGui = item.NgayGui,
                maTourTao = FixedLengthHelper.TrimSafe(item.MaTourTao)
            })
            .ToListAsync();

        return Ok(requests);
    }

    [HttpGet("{maYeuCau}")]
    public async Task<ActionResult> GetMyRequest(string maYeuCau)
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maYeuCauDb = FixedLengthHelper.PadTo20(maYeuCau);

        var request = await _context.YeuCauThietKes
            .AsNoTracking()
            .Where(item =>
                item.MaYeuCau == maYeuCauDb &&
                item.MaUser == maUserDb)
            .Select(item => new
            {
                maYeuCau = FixedLengthHelper.TrimSafe(item.MaYeuCau),
                diemDenMongMuon = item.DiemDenMongMuon,
                ngayDuKienDi = item.NgayDuKienDi,
                soNgay = item.SoNgay,
                soNguoiLon = item.SoNguoiLon,
                soTreEm = item.SoTreEm,
                nganSachDuKien = item.NganSachDuKien,
                soThichGhiChu = item.SoThichGhiChu,
                maGoiYThamKhao = FixedLengthHelper.TrimSafe(item.MaGoiYthamKhao),
                lyDoTuChoiGoiY = item.LyDoTuChoiGoiY,
                trangThai = FixedLengthHelper.TrimSafe(item.TrangThai),
                ngayGui = item.NgayGui,
                maTourTao = FixedLengthHelper.TrimSafe(item.MaTourTao)
            })
            .FirstOrDefaultAsync();

        if (request is null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy yêu cầu thiết kế của bạn."
            });
        }

        return Ok(request);
    }

    [HttpPost]
    public async Task<ActionResult> CreateRequest(
        YeuCauThietKeCreateDto request)
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        if (request.SoNguoiLon < 0 ||
            request.SoTreEm < 0 ||
            request.SoNguoiLon + request.SoTreEm <= 0)
        {
            return BadRequest(new
            {
                message = "Số người lớn và trẻ em phải hợp lệ, tổng số người phải lớn hơn 0."
            });
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);

        string? maGoiYThamKhaoDb = null;

        if (!string.IsNullOrWhiteSpace(request.MaGoiYThamKhao))
        {
            maGoiYThamKhaoDb =
                FixedLengthHelper.PadTo20(request.MaGoiYThamKhao);

            var goiYHopLe = await _context.AigoiYs
                .AnyAsync(item =>
                    item.MaRecommodation == maGoiYThamKhaoDb &&
                    item.MaUser == maUserDb);

            if (!goiYHopLe)
            {
                return BadRequest(new
                {
                    message = "Gợi ý tham khảo không tồn tại hoặc không thuộc tài khoản của bạn."
                });
            }
        }

        string maYeuCau;
        string maYeuCauDb;

        do
        {
            maYeuCau = $"YC{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            maYeuCauDb = FixedLengthHelper.PadTo20(maYeuCau);
        }
        while (await _context.YeuCauThietKes
            .AnyAsync(item => item.MaYeuCau == maYeuCauDb));

        var yeuCau = new YeuCauThietKe
        {
            MaYeuCau = maYeuCauDb,
            MaUser = maUserDb,
            DiemDenMongMuon = request.DiemDenMongMuon?.Trim(),
            NgayDuKienDi = request.NgayDuKienDi,
            SoNgay = request.SoNgay,
            SoNguoiLon = request.SoNguoiLon,
            SoTreEm = request.SoTreEm,
            NganSachDuKien = request.NganSachDuKien,
            SoThichGhiChu = request.SoThichGhiChu?.Trim(),
            MaGoiYthamKhao = maGoiYThamKhaoDb,
            LyDoTuChoiGoiY = request.LyDoTuChoiGoiY?.Trim(),
            TrangThai = FixedLengthHelper.PadTo20("Moi"),
            NgayGui = DateTime.UtcNow,
            MaTourTao = null
        };

        _context.YeuCauThietKes.Add(yeuCau);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            maYeuCau = FixedLengthHelper.TrimSafe(yeuCau.MaYeuCau),
            diemDenMongMuon = yeuCau.DiemDenMongMuon,
            ngayDuKienDi = yeuCau.NgayDuKienDi,
            soNgay = yeuCau.SoNgay,
            soNguoiLon = yeuCau.SoNguoiLon,
            soTreEm = yeuCau.SoTreEm,
            nganSachDuKien = yeuCau.NganSachDuKien,
            soThichGhiChu = yeuCau.SoThichGhiChu,
            maGoiYThamKhao = FixedLengthHelper.TrimSafe(
                yeuCau.MaGoiYthamKhao),
            trangThai = FixedLengthHelper.TrimSafe(yeuCau.TrangThai),
            ngayGui = yeuCau.NgayGui
        });
    }

    [HttpPut("{maYeuCau}/huy")]
    public async Task<ActionResult> CancelRequest(string maYeuCau)
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maYeuCauDb = FixedLengthHelper.PadTo20(maYeuCau);

        var yeuCau = await _context.YeuCauThietKes
            .FirstOrDefaultAsync(item =>
                item.MaYeuCau == maYeuCauDb &&
                item.MaUser == maUserDb);

        if (yeuCau is null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy yêu cầu thiết kế của bạn."
            });
        }

        var trangThai = FixedLengthHelper.TrimSafe(yeuCau.TrangThai);

        if (trangThai != "Moi")
        {
            return BadRequest(new
            {
                message = "Chỉ yêu cầu đang ở trạng thái mới mới có thể hủy."
            });
        }

        yeuCau.TrangThai = FixedLengthHelper.PadTo20("Huy");

        await _context.SaveChangesAsync();

        return Ok(new
        {
            maYeuCau = FixedLengthHelper.TrimSafe(yeuCau.MaYeuCau),
            trangThai = FixedLengthHelper.TrimSafe(yeuCau.TrangThai)
        });
    }

    private string? GetCurrentMaUser()
    {
        return User.FindFirst("MaUser")?.Value;
    }

    // PUT /api/YeuCauThietKe/{maYeuCau}/gui-duyet
    [HttpPut("{maYeuCau}/gui-duyet")]
    public async Task<ActionResult> SubmitForApproval(string maYeuCau)
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maYeuCauDb = FixedLengthHelper.PadTo20(maYeuCau);

        var requestData = await _context.YeuCauThietKes
            .Where(item =>
                item.MaYeuCau == maYeuCauDb &&
                item.MaUser == maUserDb)
            .Select(item => new
            {
                Request = item,
                Tour = item.MaTourTaoNavigation
            })
            .FirstOrDefaultAsync();

        if (requestData is null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy yêu cầu thiết kế của bạn."
            });
        }

        if (requestData.Tour is null)
        {
            return BadRequest(new
            {
                message = "Yêu cầu chưa có tour tự thiết kế."
            });
        }

        if (FixedLengthHelper.TrimSafe(requestData.Tour.TrangThai) != "Nhap")
        {
            return BadRequest(new
            {
                message = "Tour không còn ở trạng thái Nhap."
            });
        }

        var coLichTrinh = await _context.LichTrinhs
            .AnyAsync(item => item.MaTour == requestData.Tour.MaTour);

        if (!coLichTrinh)
        {
            return BadRequest(new
            {
                message = "Tour phải có ít nhất một dòng lịch trình trước khi gửi duyệt."
            });
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        requestData.Tour.TrangThai =
            FixedLengthHelper.PadTo20("ChoXacNhan");

        requestData.Request.TrangThai =
            FixedLengthHelper.PadTo20("ChoDuyet");

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            maYeuCau = FixedLengthHelper.TrimSafe(requestData.Request.MaYeuCau),
            maTour = FixedLengthHelper.TrimSafe(requestData.Tour.MaTour),
            trangThaiYeuCau = FixedLengthHelper.TrimSafe(
                requestData.Request.TrangThai),
            trangThaiTour = FixedLengthHelper.TrimSafe(
                requestData.Tour.TrangThai)
        });
    }

    // PUT /api/YeuCauThietKe/{maYeuCau}/duyet
    [HttpPut("{maYeuCau}/duyet")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<ActionResult> Approve(string maYeuCau)
    {
        var maYeuCauDb = FixedLengthHelper.PadTo20(maYeuCau);

        var requestData = await _context.YeuCauThietKes
            .Where(item => item.MaYeuCau == maYeuCauDb)
            .Select(item => new
            {
                Request = item,
                Tour = item.MaTourTaoNavigation
            })
            .FirstOrDefaultAsync();

        if (requestData is null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy yêu cầu thiết kế."
            });
        }

        if (requestData.Tour is null)
        {
            return BadRequest(new
            {
                message = "Yêu cầu chưa có tour tự thiết kế."
            });
        }

        if (FixedLengthHelper.TrimSafe(requestData.Tour.TrangThai) !=
            "ChoXacNhan")
        {
            return BadRequest(new
            {
                message = "Tour chưa ở trạng thái chờ xác nhận."
            });
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        requestData.Tour.TrangThai =
            FixedLengthHelper.PadTo20("DaXacNhan");

        requestData.Request.TrangThai =
            FixedLengthHelper.PadTo20("DaDuyet");

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            maYeuCau = FixedLengthHelper.TrimSafe(requestData.Request.MaYeuCau),
            maTour = FixedLengthHelper.TrimSafe(requestData.Tour.MaTour),
            trangThaiYeuCau = FixedLengthHelper.TrimSafe(
                requestData.Request.TrangThai),
            trangThaiTour = FixedLengthHelper.TrimSafe(
                requestData.Tour.TrangThai)
        });
    }
}
