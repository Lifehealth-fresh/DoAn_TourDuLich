using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourDuLich.API.DTOs;
using TourDuLich.Application.Helpers;
using TourDuLich.Application.Services;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class YeuCauThietKeController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHanhViLogger _hanhViLogger;
    private readonly IDeXuatLichTrinhService _deXuatService;
    private readonly ILogger<YeuCauThietKeController> _logger;

    public YeuCauThietKeController(
        AppDbContext context,
        IHanhViLogger hanhViLogger,
        IDeXuatLichTrinhService deXuatService,
        ILogger<YeuCauThietKeController> logger)
    {
        _context = context;
        _hanhViLogger = hanhViLogger;
        _deXuatService = deXuatService;
        _logger = logger;
    }

    [HttpGet("cua-toi")]
    [Authorize(Roles = "KhachHang")]
    public async Task<ActionResult> GetMyRequests([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);

        var query = _context.YeuCauThietKes
            .AsNoTracking()
            .Where(item => item.MaUser == maUserDb);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var totalCount = await query.CountAsync();
        var requests = await query.OrderByDescending(item => item.NgayGui).ThenBy(item => item.MaYeuCau)
            .Skip((page - 1) * pageSize).Take(pageSize)
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
                lyDoTuChoiBoiSale = item.LyDoTuChoiBoiSale,
                trangThai = FixedLengthHelper.TrimSafe(item.TrangThai),
                ngayGui = item.NgayGui,
                maTourTao = FixedLengthHelper.TrimSafe(item.MaTourTao)
            })
            .ToListAsync();

        return Ok(new { items = requests, page, pageSize, totalCount });
    }

    [HttpGet("{maYeuCau}")]
    [Authorize(Roles = "KhachHang")]
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
                lyDoTuChoiBoiSale = item.LyDoTuChoiBoiSale,
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
    [Authorize(Roles = "KhachHang")]
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

        if (!string.IsNullOrWhiteSpace(yeuCau.MaGoiYthamKhao) &&
            !string.IsNullOrWhiteSpace(yeuCau.LyDoTuChoiGoiY))
            await _hanhViLogger.LogAsync(maUserDb, null, "TuChoiGoiY");

        // Yêu cầu đã được lưu độc lập; sinh đề xuất thất bại không làm mất yêu cầu.
        try
        {
            var proposals = await _deXuatService.GenerateAsync(yeuCau);
            if (proposals.Count == 0)
                _logger.LogWarning("Chưa ghép được đề xuất cho yêu cầu {RequestId}.", maYeuCau);
        }
        catch (Exception exception)
        {
            // Không giữ các entity đề xuất còn pending sau transaction sinh thất bại.
            _context.ChangeTracker.Clear();
            _logger.LogWarning("Sinh đề xuất thất bại cho yêu cầu {RequestId} ({ErrorType}); Sale có thể xử lý lại.",
                maYeuCau, exception.GetType().Name);
        }

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
    [Authorize(Roles = "KhachHang")]
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

        if (!YeuCauThietKeStateMachine.CanCancel(trangThai))
        {
            return Conflict(new
            {
                message = $"Không thể hủy. {YeuCauThietKeStateMachine.Describe(trangThai)}"
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

    [HttpPost("{maYeuCau}/sinh-de-xuat")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<ActionResult> GenerateProposals(string maYeuCau, CancellationToken cancellationToken)
    {
        var maYeuCauDb = FixedLengthHelper.PadTo20(maYeuCau);
        var request = await _context.YeuCauThietKes
            .FirstOrDefaultAsync(item => item.MaYeuCau == maYeuCauDb, cancellationToken);
        if (request is null)
            return NotFound(new { message = "Không tìm thấy yêu cầu thiết kế." });
        var requestState = FixedLengthHelper.TrimSafe(request.TrangThai);
        if (!YeuCauThietKeStateMachine.CanGenerateProposals(requestState))
            return Conflict(new { message = $"Không thể sinh đề xuất. {YeuCauThietKeStateMachine.Describe(requestState)}" });

        var proposals = await _deXuatService.GenerateAsync(request, cancellationToken);
        if (proposals.Count == 0)
            return BadRequest(new { message = "Không tìm thấy điểm tham quan phù hợp để sinh đề xuất." });
        return Ok(proposals.Select(ToProposalResponse));
    }

    [HttpGet("{maYeuCau}/de-xuat")]
    [Authorize(Roles = "KhachHang,Sale,Admin")]
    public async Task<ActionResult> GetProposals(string maYeuCau, CancellationToken cancellationToken)
    {
        var request = await GetRequestForViewerAsync(maYeuCau, cancellationToken);
        if (request is null)
            return NotFound(new { message = "Không tìm thấy yêu cầu thiết kế hoặc bạn không có quyền xem." });

        var proposals = await _context.LichTrinhDeXuats.AsNoTracking()
            .Where(item => item.MaYeuCau == request.MaYeuCau)
            .Include(item => item.ChiTiets)
            .OrderBy(item => item.ThuTuPhuongAn)
            .ToListAsync(cancellationToken);
        return Ok(proposals.Select(ToProposalResponse));
    }

    [HttpPut("{maYeuCau}/chon-de-xuat/{maDeXuat}")]
    [Authorize(Roles = "KhachHang")]
    public async Task<ActionResult> ChooseProposal(
        string maYeuCau, string maDeXuat, CancellationToken cancellationToken)
    {
        var maUser = GetCurrentMaUser();
        if (maUser is null)
            return Unauthorized();
        var maYeuCauDb = FixedLengthHelper.PadTo20(maYeuCau);
        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var request = await _context.YeuCauThietKes.FirstOrDefaultAsync(
            item => item.MaYeuCau == maYeuCauDb && item.MaUser == maUserDb, cancellationToken);
        if (request is null)
            return NotFound(new { message = "Không tìm thấy yêu cầu thiết kế của bạn." });
        var requestState = FixedLengthHelper.TrimSafe(request.TrangThai);
        if (!YeuCauThietKeStateMachine.CanChooseProposal(requestState, !string.IsNullOrWhiteSpace(request.MaTourTao)))
            return Conflict(new { message = $"Không thể chọn đề xuất. {YeuCauThietKeStateMachine.Describe(requestState)}" });

        var proposal = await _context.LichTrinhDeXuats
            .Include(item => item.ChiTiets)
            .FirstOrDefaultAsync(item => item.MaDeXuat == FixedLengthHelper.PadTo20(maDeXuat) &&
                                         item.MaYeuCau == maYeuCauDb &&
                                         item.TrangThai == FixedLengthHelper.PadTo20("DeXuat"), cancellationToken);
        if (proposal is null)
            return NotFound(new { message = "Không tìm thấy phương án đề xuất còn hiệu lực." });

        var maTourDb = await GenerateTourIdAsync(cancellationToken);
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var tour = new Tour
        {
            MaTour = maTourDb,
            TenTour = $"Tour tự thiết kế - {request.DiemDenMongMuon}"[..Math.Min(150, $"Tour tự thiết kế - {request.DiemDenMongMuon}".Length)],
            GiaTour = proposal.TongTienDuKien,
            ThoiGian = Math.Clamp(request.SoNgay ?? 1, 1, 30),
            Slkhach = (request.SoNguoiLon ?? 0) + (request.SoTreEm ?? 0),
            LoaiTour = FixedLengthHelper.PadTo20("TuThietKe"),
            TrangThai = FixedLengthHelper.PadTo20("Nhap")
        };
        _context.Tours.Add(tour);
        foreach (var item in proposal.ChiTiets)
        {
            _context.LichTrinhs.Add(new LichTrinh
            {
                MaLichTrinh = await GenerateScheduleIdAsync(cancellationToken),
                MaTour = maTourDb,
                NgayThu = item.NgayThu,
                ThuTuTrongNgay = item.ThuTuTrongNgay,
                MaDthamQuan = item.MaDthamQuan,
                MaSanPham = item.MaSanPham,
                SoLuong = item.SoLuong,
                DonGia = item.DonGia,
                Mota = item.Mota
            });
        }
        request.MaTourTao = maTourDb;
        request.TrangThai = FixedLengthHelper.PadTo20("DangThietKe");
        var allProposals = await _context.LichTrinhDeXuats
            .Where(item => item.MaYeuCau == maYeuCauDb)
            .ToListAsync(cancellationToken);
        foreach (var item in allProposals)
            item.TrangThai = FixedLengthHelper.PadTo20(item.MaDeXuat == proposal.MaDeXuat ? "DaChon" : "KhongChon");
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await _hanhViLogger.LogAsync(maUserDb, maTourDb, "ChonDeXuatLichTrinh");

        return StatusCode(StatusCodes.Status201Created, new
        {
            maYeuCau = FixedLengthHelper.TrimSafe(request.MaYeuCau),
            maTour = FixedLengthHelper.TrimSafe(tour.MaTour),
            maDeXuat = FixedLengthHelper.TrimSafe(proposal.MaDeXuat),
            trangThai = FixedLengthHelper.TrimSafe(tour.TrangThai),
            giaTour = tour.GiaTour
        });
    }

    [HttpPut("{maYeuCau}/tu-choi-boi-sale")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<ActionResult> RejectBySale(
        string maYeuCau, LyDoTuChoiBoiSaleDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.LyDoTuChoi))
            return BadRequest(new { message = "Lý do từ chối không được để trống." });
        var maYeuCauDb = FixedLengthHelper.PadTo20(maYeuCau);
        var data = await _context.YeuCauThietKes
            .Include(item => item.MaTourTaoNavigation)
            .FirstOrDefaultAsync(item => item.MaYeuCau == maYeuCauDb, cancellationToken);
        if (data is null)
            return NotFound(new { message = "Không tìm thấy yêu cầu thiết kế." });
        if (data.MaTourTaoNavigation is null)
            return BadRequest(new { message = "Yêu cầu chưa có tour tự thiết kế." });
        var requestState = FixedLengthHelper.TrimSafe(data.TrangThai);
        var tourState = FixedLengthHelper.TrimSafe(data.MaTourTaoNavigation.TrangThai);
        if (!YeuCauThietKeStateMachine.CanReject(requestState, tourState))
            return Conflict(new { message = $"Không thể từ chối. {YeuCauThietKeStateMachine.Describe(requestState, tourState)}" });

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        data.MaTourTaoNavigation.TrangThai = FixedLengthHelper.PadTo20("Nhap");
        data.TrangThai = FixedLengthHelper.PadTo20("CanChinhSua");
        data.LyDoTuChoiBoiSale = request.LyDoTuChoi.Trim();
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { maYeuCau = maYeuCau, trangThai = "CanChinhSua", lyDoTuChoiBoiSale = data.LyDoTuChoiBoiSale });
    }

    [HttpPut("{maYeuCau}/sua-lich-trinh")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<ActionResult> EditSchedule(
        string maYeuCau, SuaLichTrinhDto request, CancellationToken cancellationToken)
    {
        if (request.ChiTiets.Count == 0)
            return BadRequest(new { message = "Lịch trình phải có ít nhất một dòng." });
        var data = await _context.YeuCauThietKes
            .Include(item => item.MaTourTaoNavigation)
            .FirstOrDefaultAsync(item => item.MaYeuCau == FixedLengthHelper.PadTo20(maYeuCau), cancellationToken);
        if (data?.MaTourTaoNavigation is null)
            return NotFound(new { message = "Không tìm thấy tour tự thiết kế của yêu cầu." });
        var tour = data.MaTourTaoNavigation;
        var requestState = FixedLengthHelper.TrimSafe(data.TrangThai);
        var tourState = FixedLengthHelper.TrimSafe(tour.TrangThai);
        if (!YeuCauThietKeStateMachine.CanEditSchedule(requestState, tourState))
            return Conflict(new { message = $"Không thể sửa lịch trình. {YeuCauThietKeStateMachine.Describe(requestState, tourState)}" });

        var pointIds = request.ChiTiets.Where(item => !string.IsNullOrWhiteSpace(item.MaDthamQuan))
            .Select(item => FixedLengthHelper.PadTo20(item.MaDthamQuan)).Distinct().ToList();
        var productIds = request.ChiTiets.Where(item => !string.IsNullOrWhiteSpace(item.MaSanPham))
            .Select(item => FixedLengthHelper.PadTo20(item.MaSanPham)).Distinct().ToList();
        var points = await _context.DiemThamQuans.Where(item => pointIds.Contains(item.MaDthamQuan)).ToDictionaryAsync(item => item.MaDthamQuan, cancellationToken);
        var products = await _context.SanPhamDoiTacs.Where(item => productIds.Contains(item.MaSanPham)).ToDictionaryAsync(item => item.MaSanPham, cancellationToken);
        if (points.Count != pointIds.Count || products.Count != productIds.Count)
            return BadRequest(new { message = "Điểm tham quan hoặc sản phẩm trong lịch trình không tồn tại." });
        if (request.ChiTiets.Any(item => item.SoLuong <= 0 || (string.IsNullOrWhiteSpace(item.MaDthamQuan) && string.IsNullOrWhiteSpace(item.MaSanPham))))
            return BadRequest(new { message = "Mỗi dòng phải có điểm/sản phẩm và số lượng lớn hơn 0." });

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        _context.LichTrinhs.RemoveRange(_context.LichTrinhs.Where(item => item.MaTour == tour.MaTour));
        var giaTour = 0;
        foreach (var item in request.ChiTiets)
        {
            var maProduct = string.IsNullOrWhiteSpace(item.MaSanPham) ? null : FixedLengthHelper.PadTo20(item.MaSanPham);
            var donGia = maProduct is null ? 0 : products[maProduct].GiaNiemYet;
            giaTour = TuThietKeTourPricing.CalculateTotal(new[]
            {
                giaTour,
                TuThietKeTourPricing.CalculateLine(donGia, item.SoLuong)
            });
            _context.LichTrinhs.Add(new LichTrinh
            {
                MaLichTrinh = await GenerateScheduleIdAsync(cancellationToken),
                MaTour = tour.MaTour,
                NgayThu = item.NgayThu,
                ThuTuTrongNgay = item.ThuTuTrongNgay,
                MaDthamQuan = string.IsNullOrWhiteSpace(item.MaDthamQuan) ? null : FixedLengthHelper.PadTo20(item.MaDthamQuan),
                MaSanPham = maProduct,
                SoLuong = item.SoLuong,
                DonGia = donGia,
                Mota = item.Mota?.Trim()
            });
        }
        tour.GiaTour = giaTour;
        data.LyDoTuChoiBoiSale = null;
        data.TrangThai = FixedLengthHelper.PadTo20("DangThietKe");
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { maYeuCau = maYeuCau, maTour = FixedLengthHelper.TrimSafe(tour.MaTour), giaTour, soDong = request.ChiTiets.Count });
    }

    private async Task<YeuCauThietKe?> GetRequestForViewerAsync(string maYeuCau, CancellationToken cancellationToken)
    {
        var key = FixedLengthHelper.PadTo20(maYeuCau);
        if (User.IsInRole("Sale") || User.IsInRole("Admin"))
            return await _context.YeuCauThietKes.FirstOrDefaultAsync(item => item.MaYeuCau == key, cancellationToken);
        var maUser = GetCurrentMaUser();
        return maUser is null ? null : await _context.YeuCauThietKes.FirstOrDefaultAsync(
            item => item.MaYeuCau == key && item.MaUser == FixedLengthHelper.PadTo20(maUser), cancellationToken);
    }

    private async Task<string> GenerateTourIdAsync(CancellationToken cancellationToken)
        => await GenerateIdAsync("TD", id => _context.Tours.AnyAsync(item => item.MaTour == id, cancellationToken));

    private async Task<string> GenerateScheduleIdAsync(CancellationToken cancellationToken)
        => await GenerateIdAsync("LT", id => _context.LichTrinhs.AnyAsync(item => item.MaLichTrinh == id, cancellationToken));

    private static async Task<string> GenerateIdAsync(string prefix, Func<string, Task<bool>> exists)
    {
        string id;
        do
        {
            id = FixedLengthHelper.PadTo20($"{prefix}{Guid.NewGuid():N}"[..20].ToUpperInvariant());
        } while (await exists(id));
        return id;
    }

    private static object ToProposalResponse(LichTrinhDeXuat item) => new
    {
        maDeXuat = FixedLengthHelper.TrimSafe(item.MaDeXuat),
        maYeuCau = FixedLengthHelper.TrimSafe(item.MaYeuCau),
        thuTuPhuongAn = item.ThuTuPhuongAn,
        tenPhuongAn = item.TenPhuongAn,
        tongTienDuKien = item.TongTienDuKien,
        ghiChu = item.GhiChu,
        trangThai = FixedLengthHelper.TrimSafe(item.TrangThai),
        ngayTao = item.NgayTao,
        chiTiets = item.ChiTiets.OrderBy(detail => detail.NgayThu).ThenBy(detail => detail.ThuTuTrongNgay).Select(detail => new
        {
            maChiTiet = FixedLengthHelper.TrimSafe(detail.MaChiTiet),
            ngayThu = detail.NgayThu,
            thuTuTrongNgay = detail.ThuTuTrongNgay,
            maDthamQuan = FixedLengthHelper.TrimSafe(detail.MaDthamQuan),
            maSanPham = FixedLengthHelper.TrimSafe(detail.MaSanPham),
            soLuong = detail.SoLuong,
            donGia = detail.DonGia,
            thanhTien = detail.ThanhTien,
            mota = detail.Mota
        })
    };

    private string? GetCurrentMaUser()
    {
        return User.FindFirst("MaUser")?.Value;
    }

    [HttpGet("danh-sach")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<ActionResult> GetAllForStaff(CancellationToken cancellationToken)
    {
        var requests = await _context.YeuCauThietKes
            .AsNoTracking()
            .OrderByDescending(item => item.NgayGui)
            .Select(item => new
            {
                maYeuCau = FixedLengthHelper.TrimSafe(item.MaYeuCau),
                maUser = FixedLengthHelper.TrimSafe(item.MaUser),
                diemDenMongMuon = item.DiemDenMongMuon,
                ngayDuKienDi = item.NgayDuKienDi,
                soNgay = item.SoNgay,
                nganSachDuKien = item.NganSachDuKien,
                trangThai = FixedLengthHelper.TrimSafe(item.TrangThai),
                lyDoTuChoiBoiSale = item.LyDoTuChoiBoiSale,
                maTourTao = FixedLengthHelper.TrimSafe(item.MaTourTao),
                ngayGui = item.NgayGui
            })
            .ToListAsync(cancellationToken);
        return Ok(requests);
    }

    // PUT /api/YeuCauThietKe/{maYeuCau}/gui-duyet
    [HttpPut("{maYeuCau}/gui-duyet")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<ActionResult> SubmitForApproval(string maYeuCau)
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

        var requestState = FixedLengthHelper.TrimSafe(requestData.Request.TrangThai);
        var tourState = FixedLengthHelper.TrimSafe(requestData.Tour.TrangThai);
        if (!YeuCauThietKeStateMachine.CanSubmitForApproval(requestState, tourState))
            return Conflict(new { message = $"Không thể gửi duyệt. {YeuCauThietKeStateMachine.Describe(requestState, tourState)}" });

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

        var requestState = FixedLengthHelper.TrimSafe(requestData.Request.TrangThai);
        var tourState = FixedLengthHelper.TrimSafe(requestData.Tour.TrangThai);
        if (!YeuCauThietKeStateMachine.CanApprove(requestState, tourState))
            return Conflict(new { message = $"Không thể duyệt. {YeuCauThietKeStateMachine.Describe(requestState, tourState)}" });

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        if (FixedLengthHelper.TrimSafe(requestData.Tour.LoaiTour) == "TuThietKe")
        {
            requestData.Tour.TrangThai = FixedLengthHelper.PadTo20("HoatDong");
            var days = Math.Clamp(requestData.Request.SoNgay ?? 1, 1, 30);
            requestData.Tour.ThoiGian = days;
            var now = DateTime.UtcNow;
            if (!await _context.LichKhoiHanhs.AnyAsync(item => item.MaTour == requestData.Tour.MaTour && item.NgayKhoiHanh > now))
            {
                var start = requestData.Request.NgayDuKienDi?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                // Ngày dự kiến có thể đã qua trong thời gian chờ Sale xử lý.
                if (!start.HasValue || start.Value <= now)
                    start = now.AddDays(14);
                var destination = requestData.Request.DiemDenMongMuon?.Trim();
                _context.LichKhoiHanhs.Add(new LichKhoiHanh
                {
                    MaKhoiHanh = await GenerateIdAsync("KH", id => _context.LichKhoiHanhs.AnyAsync(item => item.MaKhoiHanh == id)),
                    MaTour = requestData.Tour.MaTour,
                    NgayKhoiHanh = start.Value,
                    NgayKetThuc = start.Value.AddDays(days - 1),
                    DiaDiem = destination is { Length: > 100 } ? destination[..100] : destination
                });
            }
        }
        else
        {
            requestData.Tour.TrangThai = FixedLengthHelper.PadTo20("DaXacNhan");
        }

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
