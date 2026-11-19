namespace UshiMania.Models;

public sealed class KitchenOrderItem
{
    public int MaChiTiet { get; set; }
    public int MaDonHang { get; set; }
    public string? SoHieuBan { get; set; }
    public string TenMonAn { get; set; } = string.Empty;
    public int SoLuong { get; set; }
    public string? GhiChuMon { get; set; }
    public string TienDoBep { get; set; } = string.Empty;
    public DateTime ThoiGianGoi { get; set; }
}
