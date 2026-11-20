namespace UshiMania.Models;

public sealed class DanhMucThucDon
{
    public int MaDanhMuc { get; set; }
    public string TenDanhMuc { get; set; } = string.Empty;
    public List<MonAnThucDon> MonAn { get; set; } = [];
}

public sealed class MonAnThucDon
{
    public int MaMonAn { get; set; }
    public string TenMonAn { get; set; } = string.Empty;
    public decimal DonGia { get; set; }
    public string? DuongDanAnh { get; set; }
}

public sealed class DongChiTietDonHang
{
    public int MaChiTiet { get; set; }
    public int MaMonAn { get; set; }
    public string TenMonAn { get; set; } = string.Empty;
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public string? GhiChuMon { get; set; }
    public string TienDoBep { get; set; } = "CHO_NAU";
}

public sealed class DuLieuDonHangPOS
{
    public int MaDonHang { get; set; }
    public int MaBan { get; set; }
    public string SoHieuBan { get; set; } = string.Empty;
    public DateTime ThoiGianTao { get; set; }
    public decimal TienMonAn { get; set; }
    public List<DongChiTietDonHang> ChiTiet { get; set; } = [];
}
