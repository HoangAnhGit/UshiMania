using UshiMania.DAL;

namespace UshiMania.BLL;

public sealed class HoaDonKhoBLL
{
    private static readonly HashSet<string> VALID_PAYMENT_METHODS = new(StringComparer.Ordinal)
    {
        "TIEN_MAT",
        "VIETQR",
        "THE_NGAN_HANG"
    };

    private readonly HoaDonKhoDAL hoaDonKhoDAL;

    public HoaDonKhoBLL(HoaDonKhoDAL hoaDonKhoDAL)
    {
        this.hoaDonKhoDAL = hoaDonKhoDAL;
    }

    public bool ThanhToanVaTruKho(
        int orderId,
        int? cashierId,
        string paymentMethod,
        decimal discount = 0)
    {
        if (orderId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(orderId), "Mã đơn hàng phải là số dương.");
        }

        if (cashierId is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cashierId), "Mã thu ngân phải là số dương khi cung cấp.");
        }

        if (string.IsNullOrWhiteSpace(paymentMethod) || !VALID_PAYMENT_METHODS.Contains(paymentMethod))
        {
            throw new ArgumentException("Phương thức thanh toán không được hỗ trợ.", nameof(paymentMethod));
        }

        if (discount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(discount), "Số tiền giảm giá không được âm.");
        }

        return hoaDonKhoDAL.ThanhToanVaTruKho(orderId, cashierId, paymentMethod, discount);
    }
}
