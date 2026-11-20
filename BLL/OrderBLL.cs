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
            throw new ArgumentOutOfRangeException(nameof(orderId), "Order ID must be positive.");
        }

        if (menuItemId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(menuItemId), "Menu item ID must be positive.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        if (note?.Length > 200)
        {
            throw new ArgumentException("Dish note cannot exceed 200 characters.", nameof(note));
        }

        return orderDAL.AddItem(orderId, menuItemId, quantity, note);
    }

    public int? AddGuestOrder(int tableId, IReadOnlyList<GuestOrderItemRequest>? items)
    {
        if (tableId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tableId), "Table ID must be positive.");
        }

        if (items is null || items.Count == 0)
        {
            throw new ArgumentException("At least one menu item is required.", nameof(items));
        }

        foreach (var item in items)
        {
            if (item.MaMonAn <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(items), "Menu item ID must be positive.");
            }

            if (item.SoLuong <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(items), "Quantity must be positive.");
            }

            if (item.GhiChuMon?.Length > 200)
            {
                throw new ArgumentException("Dish note cannot exceed 200 characters.", nameof(items));
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
            throw new ArgumentOutOfRangeException(nameof(tableId), "Table ID must be positive.");
        }

        return orderDAL.GetOpenOrderForTable(tableId);
    }

    public bool UpdateKitchenProgress(int detailId, string progress)
    {
        if (detailId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(detailId), "Order detail ID must be positive.");
        }

        if (string.IsNullOrWhiteSpace(progress) || !VALID_KITCHEN_PROGRESS.Contains(progress))
        {
            throw new ArgumentException("Unsupported kitchen progress.", nameof(progress));
        }

        return orderDAL.UpdateKitchenProgress(detailId, progress);
    }
}
