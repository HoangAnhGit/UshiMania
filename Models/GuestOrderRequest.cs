namespace UshiMania.Models;

public sealed class ChiTietGoiMonKhach
{
    public int MaMonAn { get; set; }
    public int SoLuong { get; set; }
    public string? GhiChuMon { get; set; }
}

public sealed class YeuCauGoiMonKhach
{
    public int MaBan { get; set; }
    public List<ChiTietGoiMonKhach> Items { get; set; } = [];
}
