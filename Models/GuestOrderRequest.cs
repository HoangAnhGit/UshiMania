namespace UshiMania.Models;

public sealed class GuestOrderRequest
{
    public int MaBan { get; set; }
    public List<GuestOrderItemRequest> Items { get; set; } = [];
}

public sealed class GuestOrderItemRequest
{
    public int MaMonAn { get; set; }
    public int SoLuong { get; set; }
    public string? GhiChuMon { get; set; }
}
