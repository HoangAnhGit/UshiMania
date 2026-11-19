namespace UshiMania.Models;

public sealed class CanhBaoTonKho
{
    public int MaNguyenLieu { get; set; }
    public string TenNguyenLieu { get; set; } = string.Empty;
    public string DonViTinh { get; set; } = string.Empty;
    public decimal SoLuongTon { get; set; }
    public decimal NguongToiThieu { get; set; }
}
