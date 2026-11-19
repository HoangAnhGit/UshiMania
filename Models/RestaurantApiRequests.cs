namespace UshiMania.Models;

public sealed class CapNhatTrangThaiBanRequest
{
    public int MaBan { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}

public sealed class TaoHoacCapNhatOrderRequest
{
    public int? MaBan { get; set; }
    public int? MaDonHang { get; set; }
    public int? MaNhanVien { get; set; }
    public int? MaMonAn { get; set; }
    public int? SoLuong { get; set; }
    public string? GhiChuMon { get; set; }
}

public sealed class CapNhatTienDoBepRequest
{
    public int MaChiTiet { get; set; }
    public string TienDoBep { get; set; } = string.Empty;
}

public sealed class ThanhToanVaTruKhoRequest
{
    public int MaDonHang { get; set; }
    public int? MaThuNgan { get; set; }
    public string HinhThucTra { get; set; } = string.Empty;
    public decimal TienGiamGia { get; set; }
}
