using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TourDuLich.API.Authorization;
using TourDuLich.API.Hubs;
using TourDuLich.Application.Helpers;
using TourDuLich.Application.Services;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.API.Controllers;

[ApiController]
[Route("api/HoTro")]
[Authorize]
public class HoTroController : ControllerBase
{
    private readonly IDesignChatService _chat;
    private readonly AppDbContext _context;
    private readonly IHubContext<HoTroHub> _hub;

    public HoTroController(IDesignChatService chat, AppDbContext context, IHubContext<HoTroHub> hub)
    {
        _chat = chat;
        _context = context;
        _hub = hub;
    }

    [HttpPost("chat")]
    public async Task<ActionResult> Chat([FromBody] DesignChatRequest request, CancellationToken cancellationToken)
    {
        var maUser = User.FindFirst("MaUser")?.Value;
        if (string.IsNullOrWhiteSpace(maUser)) return Unauthorized();
        try
        {
            return Ok(await _chat.StartOrContinueAsync(FixedLengthHelper.PadTo20(maUser), request, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpGet("cua-toi")]
    [Authorize(Roles = "KhachHang")]
    public async Task<ActionResult> Mine([FromQuery] bool danhDauDoc = false)
    {
        var maUserDb = CurrentUserDb();
        if (maUserDb is null) return Unauthorized();
        var thread = await OpenOrGetAsync(maUserDb);
        return Ok(await ToThreadAsync(thread, readerIsStaff: false, danhDauDoc: danhDauDoc));
    }

    [HttpPost("cua-toi/phien-moi")]
    [Authorize(Roles = "KhachHang")]
    public async Task<ActionResult> NewSession()
    {
        var maUserDb = CurrentUserDb();
        if (maUserDb is null) return Unauthorized();
        await CloseOpenThreadsAsync(maUserDb);
        var thread = await OpenOrGetAsync(maUserDb);
        return Ok(await ToThreadAsync(thread, readerIsStaff: false, danhDauDoc: false));
    }

    [HttpPost("cua-toi")]
    [Authorize(Roles = "KhachHang")]
    public async Task<ActionResult> CustomerSend([FromBody] HoTroMessageDto request)
    {
        var maUserDb = CurrentUserDb();
        if (maUserDb is null) return Unauthorized();
        var text = request.NoiDung?.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return BadRequest(new { message = "Nội dung không được để trống." });
        if (text.Length > 2000)
            return BadRequest(new { message = "Tin nhắn tối đa 2000 ký tự." });

        var thread = await OpenOrGetAsync(maUserDb);
        if (thread.TrangThai.Trim() == "Dong")
            thread.TrangThai = "Mo";
        _context.TinNhanHoTros.Add(new TinNhanHoTro
        {
            MaTinNhan = await NewIdAsync(),
            MaCuoc = thread.MaCuoc,
            MaUserGui = maUserDb,
            VaiTroGui = "KhachHang",
            NoiDung = text,
            ThoiGian = DateTime.UtcNow,
            DaDoc = false
        });
        thread.ThoiGianCapNhat = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        var payload = await ToThreadAsync(thread, readerIsStaff: false, danhDauDoc: true);
        await NotifyAsync(thread);
        return Ok(payload);
    }

    [HttpGet("quan-ly")]
    [Authorize(Roles = "Sale,Admin")]
    [RequirePermission(PermissionCatalog.HoTro, PermissionCatalog.Xem)]
    public async Task<ActionResult> StaffInbox()
    {
        var rows = await _context.CuocTroChuyens
            .AsNoTracking()
            .OrderByDescending(item => item.ThoiGianCapNhat)
            .Take(80)
            .ToListAsync();
        var khachIds = rows.Select(r => r.MaUserKhach).Distinct().ToList();
        var guests = await _context.KhachHangs.AsNoTracking()
            .Where(k => khachIds.Contains(k.MaUser))
            .Select(k => new { k.MaUser, k.Ho, k.Ten, k.SoDienThoai })
            .ToListAsync();
        var users = await _context.NguoiSuDungs.AsNoTracking()
            .Where(u => khachIds.Contains(u.MaUser))
            .Select(u => new { u.MaUser, u.SoDienThoai })
            .ToListAsync();
        var unread = await _context.TinNhanHoTros.AsNoTracking()
            .Where(t => t.VaiTroGui == "KhachHang" && !t.DaDoc)
            .GroupBy(t => t.MaCuoc)
            .Select(g => new { MaCuoc = g.Key, So = g.Count() })
            .ToListAsync();

        return Ok(rows.Select(row =>
        {
            var guest = guests.FirstOrDefault(g => g.MaUser == row.MaUserKhach);
            var user = users.FirstOrDefault(u => u.MaUser == row.MaUserKhach);
            return new
            {
                maCuoc = FixedLengthHelper.TrimSafe(row.MaCuoc),
                tieuDe = row.TieuDe,
                trangThai = row.TrangThai.Trim(),
                thoiGianCapNhat = row.ThoiGianCapNhat,
                soChuaDoc = unread.FirstOrDefault(u => u.MaCuoc == row.MaCuoc)?.So ?? 0,
                khach = new
                {
                    maUser = FixedLengthHelper.TrimSafe(row.MaUserKhach),
                    hoTen = guest is null ? "Khách ANAM" : $"{guest.Ho} {guest.Ten}".Trim(),
                    soDienThoai = FixedLengthHelper.TrimSafe(guest?.SoDienThoai ?? user?.SoDienThoai)
                }
            };
        }));
    }

    [HttpGet("quan-ly/{maCuoc}")]
    [Authorize(Roles = "Sale,Admin")]
    [RequirePermission(PermissionCatalog.HoTro, PermissionCatalog.Xem)]
    public async Task<ActionResult> StaffThread(string maCuoc)
    {
        var thread = await _context.CuocTroChuyens
            .FirstOrDefaultAsync(item => item.MaCuoc == FixedLengthHelper.PadTo20(maCuoc));
        if (thread is null)
            return NotFound(new { message = "Không tìm thấy cuộc trò chuyện." });
        return Ok(await ToThreadAsync(thread, readerIsStaff: true, includeGuest: true, danhDauDoc: true));
    }

    [HttpPost("quan-ly/{maCuoc}")]
    [Authorize(Roles = "Sale,Admin")]
    [RequirePermission(PermissionCatalog.HoTro, PermissionCatalog.Them)]
    public async Task<ActionResult> StaffReply(string maCuoc, [FromBody] HoTroMessageDto request)
    {
        var maUserDb = CurrentUserDb();
        if (maUserDb is null) return Unauthorized();
        var text = request.NoiDung?.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return BadRequest(new { message = "Nội dung không được để trống." });
        if (text.Length > 2000)
            return BadRequest(new { message = "Tin nhắn tối đa 2000 ký tự." });

        var thread = await _context.CuocTroChuyens
            .FirstOrDefaultAsync(item => item.MaCuoc == FixedLengthHelper.PadTo20(maCuoc));
        if (thread is null)
            return NotFound(new { message = "Không tìm thấy cuộc trò chuyện." });

        thread.MaUserNhanVien ??= maUserDb;
        thread.TrangThai = "Mo";
        thread.ThoiGianCapNhat = DateTime.UtcNow;
        _context.TinNhanHoTros.Add(new TinNhanHoTro
        {
            MaTinNhan = await NewIdAsync(),
            MaCuoc = thread.MaCuoc,
            MaUserGui = maUserDb,
            VaiTroGui = "NhanVien",
            NoiDung = text,
            ThoiGian = DateTime.UtcNow,
            DaDoc = false
        });
        await _context.SaveChangesAsync();
        var payload = await ToThreadAsync(thread, readerIsStaff: true, includeGuest: true, danhDauDoc: true);
        await NotifyAsync(thread);
        return Ok(payload);
    }

    private string? CurrentUserDb()
    {
        var maUser = User.FindFirst("MaUser")?.Value;
        return string.IsNullOrWhiteSpace(maUser) ? null : FixedLengthHelper.PadTo20(maUser);
    }

    private async Task<CuocTroChuyen> OpenOrGetAsync(string maUserKhach)
    {
        var existing = await _context.CuocTroChuyens
            .Where(item => item.MaUserKhach == maUserKhach && item.TrangThai.Trim() == "Mo")
            .OrderByDescending(item => item.ThoiGianCapNhat)
            .FirstOrDefaultAsync();
        if (existing is not null) return existing;
        var created = new CuocTroChuyen
        {
            MaCuoc = await NewIdAsync(),
            MaUserKhach = maUserKhach,
            TieuDe = "Hỗ trợ khách hàng",
            TrangThai = "Mo",
            ThoiGianTao = DateTime.UtcNow,
            ThoiGianCapNhat = DateTime.UtcNow
        };
        _context.CuocTroChuyens.Add(created);
        await _context.SaveChangesAsync();
        return created;
    }

    private async Task CloseOpenThreadsAsync(string maUserKhach)
    {
        var open = await _context.CuocTroChuyens
            .Where(item => item.MaUserKhach == maUserKhach && item.TrangThai.Trim() == "Mo")
            .ToListAsync();
        foreach (var thread in open)
        {
            thread.TrangThai = "Dong";
            thread.ThoiGianCapNhat = DateTime.UtcNow;
        }
        if (open.Count > 0) await _context.SaveChangesAsync();
    }

    private async Task<object> ToThreadAsync(CuocTroChuyen thread, bool readerIsStaff, bool includeGuest = false, bool danhDauDoc = false)
    {
        var incoming = readerIsStaff ? "KhachHang" : "NhanVien";
        if (danhDauDoc)
        {
            var unread = await _context.TinNhanHoTros
                .Where(item => item.MaCuoc == thread.MaCuoc && item.VaiTroGui == incoming && !item.DaDoc)
                .ToListAsync();
            foreach (var item in unread) item.DaDoc = true;
            if (unread.Count > 0) await _context.SaveChangesAsync();
        }

        var messages = await _context.TinNhanHoTros.AsNoTracking()
            .Where(item => item.MaCuoc == thread.MaCuoc)
            .OrderBy(item => item.ThoiGian)
            .Select(item => new
            {
                maTinNhan = FixedLengthHelper.TrimSafe(item.MaTinNhan),
                vaiTro = item.VaiTroGui,
                noiDung = item.NoiDung,
                thoiGian = item.ThoiGian,
                cuaToi = item.MaUserGui,
                daDoc = item.DaDoc
            })
            .ToListAsync();

        object? khach = null;
        if (includeGuest)
        {
            var profile = await _context.KhachHangs.AsNoTracking()
                .FirstOrDefaultAsync(item => item.MaUser == thread.MaUserKhach);
            var user = await _context.NguoiSuDungs.AsNoTracking()
                .FirstAsync(item => item.MaUser == thread.MaUserKhach);
            khach = new
            {
                maUser = FixedLengthHelper.TrimSafe(thread.MaUserKhach),
                hoTen = profile is null ? "Khách ANAM" : $"{profile.Ho} {profile.Ten}".Trim(),
                soDienThoai = FixedLengthHelper.TrimSafe(profile?.SoDienThoai ?? user.SoDienThoai)
            };
        }

        var me = CurrentUserDb();
        return new
        {
            maCuoc = FixedLengthHelper.TrimSafe(thread.MaCuoc),
            tieuDe = thread.TieuDe,
            trangThai = thread.TrangThai.Trim(),
            thoiGianCapNhat = thread.ThoiGianCapNhat,
            khach,
            tinNhans = messages.Select(item => new
            {
                item.maTinNhan,
                item.vaiTro,
                item.noiDung,
                item.thoiGian,
                item.daDoc,
                cuaToi = item.cuaToi == me
            }),
            soChuaDoc = messages.Count(item => item.vaiTro == incoming && !item.daDoc)
        };
    }

    private async Task NotifyAsync(CuocTroChuyen thread)
    {
        var maCuoc = FixedLengthHelper.TrimSafe(thread.MaCuoc);
        var payload = new { maCuoc };
        var key = thread.MaUserKhach.Trim();
        await _hub.Clients.Group($"user:{key}").SendAsync("hotro", payload);
        await _hub.Clients.Group("staff").SendAsync("hotro", payload);
    }

    private async Task<string> NewIdAsync()
    {
        string id;
        do
        {
            id = FixedLengthHelper.PadTo20(Guid.NewGuid().ToString("N")[..20].ToUpperInvariant());
        }
        while (await _context.CuocTroChuyens.AnyAsync(item => item.MaCuoc == id)
            || await _context.TinNhanHoTros.AnyAsync(item => item.MaTinNhan == id)
            || await _context.NguoiSuDungs.AnyAsync(item => item.MaUser == id));
        return id;
    }
}

public class HoTroMessageDto
{
    public string NoiDung { get; set; } = null!;
}
