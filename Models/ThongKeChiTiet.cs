namespace UshiMania.Models;

public sealed class MonBanChay
{
    public int MaMonAn { get; set; }
    public string TenMonAn { get; set; } = string.Empty;
    public long SoLuongBan { get; set; }
    public decimal DoanhThu { get; set; }
}

public sealed class LichSuTruKho
{
    public int MaNhatKy { get; set; }
    public string TenNguyenLieu { get; set; } = string.Empty;
    public int? MaDonHang { get; set; }
    public string LoaiBienDong { get; set; } = string.Empty;
    public decimal SoLuongThayDoi { get; set; }
    public DateTime ThoiGian { get; set; }
}
