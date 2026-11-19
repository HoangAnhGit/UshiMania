namespace UshiMania.Models;

public sealed class MenuCategory
{
    public int MaDanhMuc { get; set; }
    public string TenDanhMuc { get; set; } = string.Empty;
    public List<MenuItem> MonAn { get; set; } = [];
}

public sealed class MenuItem
{
    public int MaMonAn { get; set; }
    public string TenMonAn { get; set; } = string.Empty;
    public decimal DonGia { get; set; }
    public string? DuongDanAnh { get; set; }
}

public sealed class OrderPOSData
{
    public int MaDonHang { get; set; }
    public int MaBan { get; set; }
    public string SoHieuBan { get; set; } = string.Empty;
    public DateTime ThoiGianTao { get; set; }
    public decimal TienMonAn { get; set; }
    public List<OrderPOSLine> ChiTiet { get; set; } = [];
}

public sealed class OrderPOSLine
{
    public int MaChiTiet { get; set; }
    public int MaMonAn { get; set; }
    public string TenMonAn { get; set; } = string.Empty;
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public string? GhiChuMon { get; set; }
}
