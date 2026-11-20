namespace UshiMania.Models;

/// <summary>
/// Món ăn chờ Bếp KDS chế biến
/// </summary>
public sealed class MonAnChoCheBien
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
