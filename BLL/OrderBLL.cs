using UshiMania.DAL;
using UshiMania.Models;

namespace UshiMania.BLL;

public sealed class OrderBLL
{
    private static readonly HashSet<string> VALID_KITCHEN_PROGRESS = new(StringComparer.Ordinal)
    {
        "CHO_NAU",
        "DANG_NAU",
        "DA_XONG"
    };

    private readonly OrderDAL orderDAL;

    public OrderBLL(OrderDAL orderDAL)
    {
        this.orderDAL = orderDAL;
    }

    public int? CreateOrder(int tableId, int? employeeId)
    {
        if (tableId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tableId), "Table ID must be positive.");
        }

        if (employeeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(employeeId), "Employee ID must be positive when supplied.");
        }

        return orderDAL.CreateOrder(tableId, employeeId);
    }

    public int? AddItem(int orderId, int menuItemId, int quantity, string? note)
    {
        if (orderId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(orderId), "Mã đơn hàng phải là số dương.");
        }

        if (menuItemId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(menuItemId), "Mã món ăn phải là số dương.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Số lượng phải là số dương.");
        }

        if (note?.Length > 200)
        {
            throw new ArgumentException("Ghi chú món không được vượt quá 200 ký tự.", nameof(note));
        }

        return orderDAL.AddItem(orderId, menuItemId, quantity, note);
    }

    public bool CapNhatChiTietOrder(int detailId, int quantity, string? note)
    {
        if (detailId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(detailId), "Order detail ID must be positive.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        if (note?.Length > 200)
        {
            throw new ArgumentException("Dish note cannot exceed 200 characters.", nameof(note));
        }

        return orderDAL.CapNhatChiTietOrder(detailId, quantity, note);
    }

    public bool XoaChiTietOrder(int detailId)
    {
        if (detailId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(detailId), "Order detail ID must be positive.");
        }

        return orderDAL.XoaChiTietOrder(detailId);
    }

    public int? AddGuestOrder(int tableId, IReadOnlyList<GuestOrderItemRequest>? items)
    {
        if (tableId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tableId), "Mã bàn phải là số dương.");
        }

        if (items is null || items.Count == 0)
        {
            throw new ArgumentException("Cần chọn ít nhất một món ăn.", nameof(items));
        }

        foreach (var item in items)
        {
            if (item.MaMonAn <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(items), "Mã món ăn phải là số dương.");
            }

            if (item.SoLuong <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(items), "Số lượng phải là số dương.");
            }

            if (item.GhiChuMon?.Length > 200)
            {
                throw new ArgumentException("Ghi chú món không được vượt quá 200 ký tự.", nameof(items));
            }
        }

        return orderDAL.AddGuestOrder(tableId, items);
    }

    public List<KitchenOrderItem> GetKitchenItems()
    {
        return orderDAL.GetKitchenItems();
    }

    public List<MenuCategory> GetMenuCategories()
    {
        return orderDAL.GetMenuCategories();
    }

    public OrderPOSData? GetOpenOrderForTable(int tableId)
    {
        if (tableId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tableId), "Mã bàn phải là số dương.");
        }

        return orderDAL.GetOpenOrderForTable(tableId);
    }

    public bool UpdateKitchenProgress(int detailId, string progress)
    {
        if (detailId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(detailId), "Mã chi tiết đơn hàng phải là số dương.");
        }

        if (string.IsNullOrWhiteSpace(progress) || !VALID_KITCHEN_PROGRESS.Contains(progress))
        {
            throw new ArgumentException("Tiến độ bếp không được hỗ trợ.", nameof(progress));
        }

        return orderDAL.UpdateKitchenProgress(detailId, progress);
    }
}
