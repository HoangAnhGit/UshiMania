namespace UshiMania.Models;

public sealed class BanAn
{
    public int MaBan { get; set; }
    public string SoHieuBan { get; set; } = string.Empty;
    public string KhuVuc { get; set; } = string.Empty;
    public int SucChua { get; set; }
    public string? TrangThai { get; set; }
    public decimal TongTienTamTinh { get; set; }
}
