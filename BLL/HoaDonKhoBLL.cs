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
            throw new ArgumentOutOfRangeException(nameof(orderId), "Order ID must be positive.");
        }

        if (cashierId is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cashierId), "Cashier ID must be positive when supplied.");
        }

        if (string.IsNullOrWhiteSpace(paymentMethod) || !VALID_PAYMENT_METHODS.Contains(paymentMethod))
        {
            throw new ArgumentException("Unsupported payment method.", nameof(paymentMethod));
        }

        if (discount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(discount), "Discount cannot be negative.");
        }

        return hoaDonKhoDAL.ThanhToanVaTruKho(orderId, cashierId, paymentMethod, discount);
    }
}
