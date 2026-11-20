namespace UshiMania.Models;

/// <summary>
/// Chi tiết món ăn khách gọi qua mã QR
/// </summary>
public sealed class ChiTietGoiMonKhach
{
    public int MaMonAn { get; set; }
    public int SoLuong { get; set; }
    public string? GhiChuMon { get; set; }
}

/// <summary>
/// Yêu cầu gọi món của Khách hàng tại bàn
/// </summary>
public sealed class YeuCauGoiMonKhach
{
    public int MaBan { get; set; }
    public List<ChiTietGoiMonKhach> Items { get; set; } = [];
}
