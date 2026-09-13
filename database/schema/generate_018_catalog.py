#!/usr/bin/env python3
"""Sinh 018_TinhThanh_SampleCatalog.sql — 63 tỉnh, catalog tự thiết kế."""
from pathlib import Path

NORTH = [
    "Hà Nội", "Hải Phòng", "Quảng Ninh", "Bắc Ninh", "Hải Dương", "Hưng Yên", "Vĩnh Phúc",
    "Thái Nguyên", "Phú Thọ", "Bắc Giang", "Lạng Sơn", "Cao Bằng", "Hà Giang", "Tuyên Quang",
    "Lào Cai", "Yên Bái", "Điện Biên", "Lai Châu", "Sơn La", "Hòa Bình", "Ninh Bình",
    "Nam Định", "Thái Bình", "Hà Nam", "Bắc Kạn",
]
CENTRAL = [
    "Thanh Hóa", "Nghệ An", "Hà Tĩnh", "Quảng Bình", "Quảng Trị", "Thừa Thiên Huế",
    "Đà Nẵng", "Quảng Nam", "Quảng Ngãi", "Bình Định", "Phú Yên", "Khánh Hòa",
    "Ninh Thuận", "Bình Thuận", "Kon Tum", "Gia Lai", "Đắk Lắk", "Đắk Nông", "Lâm Đồng",
]
SOUTH = [
    "TP. Hồ Chí Minh", "Đồng Nai", "Bình Dương", "Bà Rịa - Vũng Tàu", "Tây Ninh", "Bình Phước",
    "Long An", "Tiền Giang", "Bến Tre", "Vĩnh Long", "Trà Vinh", "Đồng Tháp", "An Giang",
    "Kiên Giang", "Cần Thơ", "Hậu Giang", "Sóc Trăng", "Bạc Liêu", "Cà Mau",
]

SPECIAL_SIGHTS = {
    "Hà Nội": ["Hồ Hoàn Kiếm", "Văn Miếu Quốc Tử Giám", "Lăng Chủ tịch Hồ Chí Minh", "Phố cổ Hà Nội", "Chùa Trấn Quốc"],
    "Hải Phòng": ["Đồ Sơn", "Cát Bà", "Nhà hát lớn Hải Phòng", "Đền Nghè", "Bãi Cháy Cát Bà"],
    "Quảng Ninh": ["Vịnh Hạ Long", "Đảo Tuần Châu", "Yên Tử", "Bãi Cháy", "Cột cờ Hạ Long"],
    "Lào Cai": ["Thị trấn Sa Pa", "Fansipan", "Bản Cát Cát", "Núi Hàm Rồng", "Chợ tình Sa Pa"],
    "Ninh Bình": ["Tràng An", "Tam Cốc", "Chùa Bái Đính", "Cố đô Hoa Lư", "Hang Múa"],
    "Thừa Thiên Huế": ["Đại Nội Huế", "Chùa Thiên Mụ", "Lăng Tự Đức", "Cầu Tràng Tiền", "Đồi Vọng Cảnh"],
    "Đà Nẵng": ["Bán đảo Sơn Trà", "Bà Nà Hills", "Cầu Rồng", "Bãi biển Mỹ Khê", "Ngũ Hành Sơn"],
    "Quảng Nam": ["Phố cổ Hội An", "Thánh địa Mỹ Sơn", "Cù Lao Chàm", "Chùa Cầu", "Rừng dừa Bảy Mẫu"],
    "Khánh Hòa": ["Chùa Phước Long", "Tháp Bà Ponagar", "Đảo Hòn Mun", "Bãi biển Trần Phú", "Nhà thờ Núi Nha Trang"],
    "Lâm Đồng": ["Hồ Xuân Hương", "Thác Datanla", "Đồi chè Cầu Đất", "Thiền viện Trúc Lâm", "Ga Đà Lạt"],
    "TP. Hồ Chí Minh": ["Chợ Bến Thành", "Nhà thờ Đức Bà", "Dinh Độc Lập", "Phố đi bộ Nguyễn Huệ", "Bến Nhà Rồng"],
    "Kiên Giang": ["Phú Quốc", "Dinh Cậu", "Bãi Sao", "Hòn Thơm", "Chợ đêm Phú Quốc"],
    "Cần Thơ": ["Chợ nổi Cái Răng", "Bến Ninh Kiều", "Nhà cổ Bình Thủy", "Thiền viện Trúc Lâm Phương Nam", "Vườn cò Bằng Lăng"],
}

SPECIAL_PLAY = {
    "Khánh Hòa": ["VinWonders Nha Trang", "Tháp Đôi giải trí", "Công viên nước Nha Trang", "Khu lặn Hòn Mun", "Cáp treo Nha Trang"],
    "Đà Nẵng": ["Công viên Châu Á", "Suối khoáng Thần Tài", "Helio Center", "Bãi tắm Phạm Văn Đồng", "Sân golf Bà Nà"],
    "Hà Nội": ["Công viên Thủ Lệ", "Vinpearl Aquarium", "Công viên nước Hồ Tây", "Làng văn hóa các dân tộc", "Rạp Xiếc Trung ương"],
    "TP. Hồ Chí Minh": ["Đầm Sen", "Suối Tiên", "VinWonders Thành phố", "Crescent Mall ice rink", "The Castle amusement"],
}

SPECIAL_FOOD = {
    "Khánh Hòa": ["Vietnam AncientTown", "Nhà hàng Yến Sào", "Quán bún chả cá Nha Trang", "Hải sản Tháp Bà", "Bánh căn Nhà Zô"],
    "Hà Nội": ["Phở Gia Truyền", "Bún chả Hàng Quạt", "Chả cá Lã Vọng", "Nhà hàng quán ăn Ngon", "Cà phê Giảng"],
    "TP. Hồ Chí Minh": ["Cơm tấm Cali", "Nhà hàng Việt Phố", "Bánh mì Huỳnh Hoa", "Lẩu dê Đồng Nai", "The Deck Saigon"],
    "Quảng Nam": ["Cao lầu bà Bé", "Cơm gà Hội An", "Nhà hàng Morning Glory", "Bánh bao Bánh vạc", "Mì Quảng Bà Mua"],
}

SPECIAL_HOTELS = {
    "Khánh Hòa": ["Khách sạn Nha Trang Beach", "Liberty Central Nha Trang", "Sunrise Nha Trang", "Mia Resort Nha Trang", "Khách sạn Hòn Chồng"],
}


def esc(s: str) -> str:
    return s.replace("'", "''")


def pad(code: str) -> str:
    return code.ljust(20)


def sights_for(name: str) -> list[str]:
    if name in SPECIAL_SIGHTS:
        return SPECIAL_SIGHTS[name]
    return [
        f"Trung tâm thành phố {name}",
        f"Bảo tàng {name}",
        f"Đền / chùa cổ {name}",
        f"Công viên văn hóa {name}",
        f"Chợ đêm {name}",
    ]


def play_for(name: str) -> list[str]:
    if name in SPECIAL_PLAY:
        return SPECIAL_PLAY[name]
    return [
        f"Công viên giải trí {name}",
        f"Khu vui chơi gia đình {name}",
        f"Công viên nước {name}",
        f"Khu thể thao {name}",
        f"Quảng trường lễ hội {name}",
    ]


def food_for(name: str) -> list[str]:
    if name in SPECIAL_FOOD:
        return SPECIAL_FOOD[name]
    return [
        f"Nhà hàng đặc sản {name}",
        f"Quán cơm quê {name}",
        f"Hải sản / vườn {name}",
        f"Lẩu nướng {name}",
        f"Quán ăn gia đình {name} 2",
    ]


def hotels_for(name: str, count: int) -> list[str]:
    if name in SPECIAL_HOTELS:
        base = SPECIAL_HOTELS[name][:]
    else:
        base = [
            f"Khách sạn {name} Center",
            f"Grand Hotel {name}",
            f"Khách sạn Sông {name}",
            f"{name} Boutique Hotel",
            f"Nghỉ dưỡng {name} Garden",
        ]
    return base[:count]


def main() -> None:
    provinces: list[tuple[str, str, str]] = []
    for name in NORTH:
        provinces.append((name, "KV001", "Miền Bắc"))
    for name in CENTRAL:
        provinces.append((name, "KV002", "Miền Trung"))
    for name in SOUTH:
        provinces.append((name, "KV003", "Miền Nam"))
    assert len(provinces) == 63, len(provinces)

    lines = [
        "/* 63 tỉnh thành (chia 3 miền). Mỗi tỉnh: 5 điểm tham quan, 5 khu vui chơi, 5 quán ăn,",
        "   4-5 khách sạn × 3 loại phòng. Không sửa 001. Chạy sau 017. */",
        "SET NOCOUNT ON;",
        "IF OBJECT_ID(N'dbo.TinhThanh', N'U') IS NULL",
        "BEGIN",
        "    CREATE TABLE dbo.TinhThanh (",
        "        MaTinh nchar(20) NOT NULL CONSTRAINT PK_TinhThanh PRIMARY KEY,",
        "        TenTinh nvarchar(100) NOT NULL,",
        "        MaKhuVuc nchar(20) NOT NULL,",
        "        CONSTRAINT FK_TinhThanh_KhuVuc FOREIGN KEY (MaKhuVuc) REFERENCES dbo.KhuVuc(MaKhuVuc)",
        "    );",
        "END;",
        "GO",
        "IF COL_LENGTH(N'dbo.DiemThamQuan', N'MaTinh') IS NULL",
        "    ALTER TABLE dbo.DiemThamQuan ADD MaTinh nchar(20) NULL;",
        "GO",
        "IF COL_LENGTH(N'dbo.DoiTac', N'MaTinh') IS NULL",
        "    ALTER TABLE dbo.DoiTac ADD MaTinh nchar(20) NULL;",
        "GO",
        "IF COL_LENGTH(N'dbo.LichTrinhDeXuatChiTiet', N'GioBatDau') IS NULL",
        "    ALTER TABLE dbo.LichTrinhDeXuatChiTiet ADD GioBatDau time(0) NULL;",
        "GO",
        "IF OBJECT_ID(N'dbo.FK_DiemThamQuan_TinhThanh', N'F') IS NULL",
        "    ALTER TABLE dbo.DiemThamQuan ADD CONSTRAINT FK_DiemThamQuan_TinhThanh",
        "        FOREIGN KEY (MaTinh) REFERENCES dbo.TinhThanh(MaTinh);",
        "GO",
        "IF OBJECT_ID(N'dbo.FK_DoiTac_TinhThanh', N'F') IS NULL",
        "    ALTER TABLE dbo.DoiTac ADD CONSTRAINT FK_DoiTac_TinhThanh",
        "        FOREIGN KEY (MaTinh) REFERENCES dbo.TinhThanh(MaTinh);",
        "GO",
    ]

    tinh_rows = []
    for i, (name, kv, _) in enumerate(provinces, start=1):
        ma = f"TN{i:02d}"
        tinh_rows.append(
            f"    (N'{pad(ma)}', N'{esc(name)}', N'{pad(kv)}')"
        )
    lines.append("INSERT INTO dbo.TinhThanh (MaTinh, TenTinh, MaKhuVuc)")
    lines.append("SELECT v.MaTinh, v.TenTinh, v.MaKhuVuc FROM (VALUES")
    lines.append(",\n".join(tinh_rows))
    lines.append(") v(MaTinh, TenTinh, MaKhuVuc)")
    lines.append("WHERE NOT EXISTS (SELECT 1 FROM dbo.TinhThanh x WHERE x.MaTinh = v.MaTinh);")
    lines.append("GO")

    diem_rows = []
    doi_rows = []
    sp_rows = []

    for i, (name, kv, _) in enumerate(provinces, start=1):
        ma_tinh = pad(f"TN{i:02d}")
        kv_p = pad(kv)
        hotel_count = 4 if i % 2 == 0 else 5
        sights = sights_for(name)
        plays = play_for(name)
        foods = food_for(name)
        hotels = hotels_for(name, hotel_count)

        for j, sight in enumerate(sights, start=1):
            ma_d = pad(f"DTV{i:02d}{j}")
            ticket = 0 if j == 1 else (20000 * j)
            diem_rows.append(
                f"    (N'{ma_d}', N'{esc(sight)}', N'{esc(name)}', N'{kv_p}', N'{ma_tinh}', "
                f"CAST(106.5 AS decimal(9,6)), CAST(16.0 AS decimal(9,6)), N'Điểm tham quan tại {esc(name)}')"
            )
            ma_dt = pad(f"DTV{i:02d}P")
            if j == 1:
                doi_rows.append(
                    f"    (N'{ma_dt}', N'Dịch vụ tham quan {esc(name)}', N'{pad('HoatDong')}', "
                    f"N'Điều hành', N'0901{i:02d}0001', N'thamquan{i:02d}@anam.vn', N'{kv_p}', "
                    f"CAST(8.00 AS decimal(5,2)), N'{pad('HoatDong')}', N'{ma_tinh}')"
                )
            ma_sp = pad(f"SPV{i:02d}{j}")
            diem_fk = ma_d
            sp_rows.append(
                f"    (N'{ma_sp}', N'{ma_dt}', N'Vé {esc(sight)}', N've', {ticket}, N'{diem_fk}', "
                f"N'Vé tham quan (có thể 0đ)', N'{pad('HoatDong')}')"
            )

        ma_play_dt = pad(f"DTP{i:02d}P")
        doi_rows.append(
            f"    (N'{ma_play_dt}', N'Khu vui chơi {esc(name)}', N'{pad('HoatDong')}', "
            f"N'Điều hành', N'0902{i:02d}0001', N'vuichoi{i:02d}@anam.vn', N'{kv_p}', "
            f"CAST(10.00 AS decimal(5,2)), N'{pad('HoatDong')}', N'{ma_tinh}')"
        )
        for j, play in enumerate(plays, start=1):
            ma_d = pad(f"DTP{i:02d}{j}")
            price = 80000 + j * 40000
            diem_rows.append(
                f"    (N'{ma_d}', N'{esc(play)}', N'{esc(name)}', N'{kv_p}', N'{ma_tinh}', "
                f"CAST(106.6 AS decimal(9,6)), CAST(16.1 AS decimal(9,6)), N'Khu vui chơi tại {esc(name)}')"
            )
            sp_rows.append(
                f"    (N'{pad(f'SPP{i:02d}{j}')}', N'{ma_play_dt}', N'Vé {esc(play)}', N've', {price}, "
                f"N'{ma_d}', N'Vé khu vui chơi', N'{pad('HoatDong')}')"
            )

        for j, food in enumerate(foods, start=1):
            ma_dt = pad(f"DTF{i:02d}{j}")
            doi_rows.append(
                f"    (N'{ma_dt}', N'{esc(food)}', N'{pad('AnUong')}', N'Bếp trưởng', "
                f"N'0903{i:02d}{j:02d}01', N'an{i:02d}{j}@anam.vn', N'{kv_p}', "
                f"CAST(12.00 AS decimal(5,2)), N'{pad('HoatDong')}', N'{ma_tinh}')"
            )
            price = 90000 + j * 30000
            sp_rows.append(
                f"    (N'{pad(f'SPF{i:02d}{j}')}', N'{ma_dt}', N'Suất ăn {esc(food)}', N'suat', {price}, "
                f"NULL, N'Bữa ăn đặc sản', N'{pad('HoatDong')}')"
            )

        rooms = [
            ("Phòng tiêu chuẩn", 650000 + (i % 7) * 20000),
            ("Phòng Deluxe", 1250000 + (i % 5) * 50000),
            ("Phòng Suite", 2100000 + (i % 4) * 100000),
        ]
        for j, hotel in enumerate(hotels, start=1):
            ma_dt = pad(f"DTH{i:02d}{j}")
            doi_rows.append(
                f"    (N'{ma_dt}', N'{esc(hotel)}', N'{pad('LuuTru')}', N'Lễ tân', "
                f"N'0904{i:02d}{j:02d}01', N'ks{i:02d}{j}@anam.vn', N'{kv_p}', "
                f"CAST(10.00 AS decimal(5,2)), N'{pad('HoatDong')}', N'{ma_tinh}')"
            )
            for k, (room, price) in enumerate(rooms, start=1):
                sp_rows.append(
                    f"    (N'{pad(f'SPR{i:02d}{j}{k}')}', N'{ma_dt}', N'{esc(room)}', N'dem', {price}, "
                    f"NULL, N'Giá 1 đêm, gồm ăn sáng', N'{pad('HoatDong')}')"
                )

    def chunked_insert(table, cols, rows, alias="v"):
        out = []
        size = 80
        for start in range(0, len(rows), size):
            part = rows[start:start + size]
            out.append(f"INSERT INTO {table} ({cols})")
            out.append(f"SELECT * FROM (VALUES")
            out.append(",\n".join(part))
            pk = cols.split(",")[0].strip()
            out.append(f") {alias}({cols})")
            out.append(f"WHERE NOT EXISTS (SELECT 1 FROM {table} x WHERE x.{pk} = {alias}.{pk});")
            out.append("GO")
        return out

    lines += chunked_insert(
        "dbo.DiemThamQuan",
        "MaDThamQuan, TenDiaDanh, DiaChi, MaKhuVuc, MaTinh, KinhDo, ViDo, Mota",
        diem_rows,
    )
    lines += chunked_insert(
        "dbo.DoiTac",
        "MaDoiTac, TenDoiTac, LoaiDoiTac, NguoiLienHe, SoDienThoai, Email, MaKhuVuc, PhanTramHoaHong, TrangThai, MaTinh",
        doi_rows,
    )
    lines += chunked_insert(
        "dbo.SanPhamDoiTac",
        "MaSanPham, MaDoiTac, TenSanPham, DonViTinh, GiaNiemYet, MaDThamQuan, Mota, TrangThai",
        sp_rows,
    )
    lines.append("UPDATE dbo.DiemThamQuan SET MaTinh = N'TN01' WHERE MaDThamQuan IN (N'DT001', N'DT002') AND MaTinh IS NULL;")
    lines.append("UPDATE dbo.DiemThamQuan SET MaTinh = N'TN15' WHERE TenDiaDanh LIKE N'%Sa Pa%' AND MaTinh IS NULL;")
    lines.append("UPDATE dbo.DiemThamQuan SET MaTinh = N'TN21' WHERE TenDiaDanh IN (N'Tràng An', N'Tam Cốc') AND MaTinh IS NULL;")
    lines.append("UPDATE dbo.DoiTac SET MaTinh = N'TN01' WHERE MaDoiTac = N'DTAC001' AND MaTinh IS NULL;")
    lines.append("GO")

    dest = Path(__file__).with_name("018_TinhThanh_SampleCatalog.sql")
    dest.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"Wrote {dest} provinces={len(provinces)} diem={len(diem_rows)} doitac={len(doi_rows)} sp={len(sp_rows)}")


if __name__ == "__main__":
    main()
