using System.Data;
using UshiMania.Models;

namespace UshiMania.DAL;

public sealed class OrderDAL
{
    private const string OPEN_ORDER_STATUS = "DANG_MO";
    private readonly DbHelper dbHelper;

    public OrderDAL(DbHelper dbHelper)
    {
        this.dbHelper = dbHelper;
    }

    public int? CreateOrder(int tableId, int? employeeId)
    {
        using var connection = dbHelper.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            const string lockTableSql = """
                SELECT trang_thai
                FROM ban_an WITH (UPDLOCK, HOLDLOCK)
                WHERE ma_ban = @ma_ban;
                """;

            string? tableStatus;
            using (var lockCommand = dbHelper.CreateCommand(connection, lockTableSql, transaction))
            {
                lockCommand.Parameters.AddWithValue("@ma_ban", tableId);
                var result = lockCommand.ExecuteScalar();
                if (result is null)
                {
                    transaction.Rollback();
                    return null;
                }

                tableStatus = result is DBNull ? null : Convert.ToString(result);
            }

            if (tableStatus is "DA_DAT" or "CHO_THANH_TOAN")
            {
                transaction.Rollback();
                return null;
            }

            const string activeOrderSql = """
                SELECT COUNT(1)
                FROM don_hang WITH (UPDLOCK, HOLDLOCK)
                WHERE ma_ban = @ma_ban
                  AND trang_thai_don = @trang_thai_don;
                """;

            using (var activeOrderCommand = dbHelper.CreateCommand(connection, activeOrderSql, transaction))
            {
                activeOrderCommand.Parameters.AddWithValue("@ma_ban", tableId);
                activeOrderCommand.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
                if (Convert.ToInt32(activeOrderCommand.ExecuteScalar()) > 0)
                {
                    transaction.Rollback();
                    return null;
                }
            }

            const string insertOrderSql = """
                INSERT INTO don_hang (ma_ban, ma_nhan_vien, trang_thai_don)
                OUTPUT INSERTED.ma_don_hang
                VALUES (@ma_ban, @ma_nhan_vien, @trang_thai_don);
                """;

            int orderId;
            using (var insertCommand = dbHelper.CreateCommand(connection, insertOrderSql, transaction))
            {
                insertCommand.Parameters.AddWithValue("@ma_ban", tableId);
                insertCommand.Parameters.AddWithValue("@ma_nhan_vien", (object?)employeeId ?? DBNull.Value);
                insertCommand.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
                orderId = Convert.ToInt32(insertCommand.ExecuteScalar());
            }

            const string updateTableSql = """
                UPDATE ban_an
                SET trang_thai = N'DANG_PHUC_VU'
                WHERE ma_ban = @ma_ban;
                """;

            using (var updateTableCommand = dbHelper.CreateCommand(connection, updateTableSql, transaction))
            {
                updateTableCommand.Parameters.AddWithValue("@ma_ban", tableId);
                updateTableCommand.ExecuteNonQuery();
            }

            transaction.Commit();
            return orderId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public int? AddItem(int orderId, int menuItemId, int quantity, string? note)
    {
        using var connection = dbHelper.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            const string insertItemSql = """
                INSERT INTO chi_tiet_don_hang
                    (ma_don_hang, ma_mon_an, so_luong, don_gia_luc_goi, ghi_chu_mon)
                OUTPUT INSERTED.ma_chi_tiet, INSERTED.don_gia_luc_goi
                SELECT d.ma_don_hang, m.ma_mon_an, @so_luong, m.don_gia, @ghi_chu_mon
                FROM don_hang AS d WITH (UPDLOCK, HOLDLOCK)
                INNER JOIN mon_an AS m ON m.ma_mon_an = @ma_mon_an
                WHERE d.ma_don_hang = @ma_don_hang
                  AND d.trang_thai_don = @trang_thai_don
                  AND m.trang_thai_ban = 1;
                """;

            int? detailId = null;
            decimal unitPrice = 0;
            using (var insertCommand = dbHelper.CreateCommand(connection, insertItemSql, transaction))
            {
                insertCommand.Parameters.AddWithValue("@so_luong", quantity);
                insertCommand.Parameters.AddWithValue("@ghi_chu_mon", (object?)note ?? DBNull.Value);
                insertCommand.Parameters.AddWithValue("@ma_mon_an", menuItemId);
                insertCommand.Parameters.AddWithValue("@ma_don_hang", orderId);
                insertCommand.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
                using var reader = insertCommand.ExecuteReader();
                if (reader.Read())
                {
                    detailId = reader.GetInt32(0);
                    unitPrice = reader.GetDecimal(1);
                }
            }

            if (detailId is null)
            {
                transaction.Rollback();
                return null;
            }

            const string updateTotalSql = """
                UPDATE don_hang
                SET tong_tien_tam = ISNULL(tong_tien_tam, 0) + @thanh_tien
                WHERE ma_don_hang = @ma_don_hang;
                """;

            using (var updateTotalCommand = dbHelper.CreateCommand(connection, updateTotalSql, transaction))
            {
                updateTotalCommand.Parameters.AddWithValue("@thanh_tien", unitPrice * quantity);
                updateTotalCommand.Parameters.AddWithValue("@ma_don_hang", orderId);
                updateTotalCommand.ExecuteNonQuery();
            }

            transaction.Commit();
            return detailId.Value;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public int? AddGuestOrder(int tableId, IReadOnlyList<GuestOrderItemRequest> items)
    {
        using var connection = dbHelper.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            const string lockTableSql = """
                SELECT trang_thai
                FROM ban_an WITH (UPDLOCK, HOLDLOCK)
                WHERE ma_ban = @ma_ban;
                """;

            object? tableStatusValue;
            using (var lockTableCommand = dbHelper.CreateCommand(connection, lockTableSql, transaction))
            {
                lockTableCommand.Parameters.AddWithValue("@ma_ban", tableId);
                tableStatusValue = lockTableCommand.ExecuteScalar();
            }

            if (tableStatusValue is null or DBNull)
            {
                transaction.Rollback();
                return null;
            }

            var tableStatus = Convert.ToString(tableStatusValue);
            if (tableStatus is "DA_DAT" or "CHO_THANH_TOAN")
            {
                transaction.Rollback();
                return null;
            }

            const string findOrderSql = """
                SELECT TOP (1) ma_don_hang
                FROM don_hang WITH (UPDLOCK, HOLDLOCK)
                WHERE ma_ban = @ma_ban
                  AND trang_thai_don = @trang_thai_don
                ORDER BY thoi_gian_tao DESC, ma_don_hang DESC;
                """;

            int orderId;
            using (var findOrderCommand = dbHelper.CreateCommand(connection, findOrderSql, transaction))
            {
                findOrderCommand.Parameters.AddWithValue("@ma_ban", tableId);
                findOrderCommand.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
                var existingOrderId = findOrderCommand.ExecuteScalar();
                if (existingOrderId is null or DBNull)
                {
                    const string insertOrderSql = """
                        INSERT INTO don_hang (ma_ban, ma_nhan_vien, trang_thai_don)
                        OUTPUT INSERTED.ma_don_hang
                        VALUES (@ma_ban, NULL, @trang_thai_don);
                        """;

                    using var insertOrderCommand = dbHelper.CreateCommand(connection, insertOrderSql, transaction);
                    insertOrderCommand.Parameters.AddWithValue("@ma_ban", tableId);
                    insertOrderCommand.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
                    orderId = Convert.ToInt32(insertOrderCommand.ExecuteScalar());
                }
                else
                {
                    orderId = Convert.ToInt32(existingOrderId);
                }
            }

            const string insertItemSql = """
                INSERT INTO chi_tiet_don_hang
                    (ma_don_hang, ma_mon_an, so_luong, don_gia_luc_goi, ghi_chu_mon)
                OUTPUT INSERTED.don_gia_luc_goi
                SELECT @ma_don_hang, m.ma_mon_an, @so_luong, m.don_gia, @ghi_chu_mon
                FROM mon_an AS m
                WHERE m.ma_mon_an = @ma_mon_an
                  AND m.trang_thai_ban = 1;
                """;

            decimal subtotalToAdd = 0;
            foreach (var item in items)
            {
                using var insertItemCommand = dbHelper.CreateCommand(connection, insertItemSql, transaction);
                insertItemCommand.Parameters.AddWithValue("@ma_don_hang", orderId);
                insertItemCommand.Parameters.AddWithValue("@ma_mon_an", item.MaMonAn);
                insertItemCommand.Parameters.AddWithValue("@so_luong", item.SoLuong);
                insertItemCommand.Parameters.AddWithValue("@ghi_chu_mon", (object?)item.GhiChuMon ?? DBNull.Value);
                var priceValue = insertItemCommand.ExecuteScalar();
                if (priceValue is null or DBNull)
                {
                    transaction.Rollback();
                    return null;
                }

                subtotalToAdd += Convert.ToDecimal(priceValue) * item.SoLuong;
            }

            const string updateOrderSql = """
                UPDATE don_hang
                SET tong_tien_tam = ISNULL(tong_tien_tam, 0) + @tien_them
                WHERE ma_don_hang = @ma_don_hang;
                """;

            using (var updateOrderCommand = dbHelper.CreateCommand(connection, updateOrderSql, transaction))
            {
                updateOrderCommand.Parameters.AddWithValue("@tien_them", subtotalToAdd);
                updateOrderCommand.Parameters.AddWithValue("@ma_don_hang", orderId);
                updateOrderCommand.ExecuteNonQuery();
            }

            const string updateTableSql = """
                UPDATE ban_an
                SET trang_thai = N'DANG_PHUC_VU'
                WHERE ma_ban = @ma_ban;
                """;

            using (var updateTableCommand = dbHelper.CreateCommand(connection, updateTableSql, transaction))
            {
                updateTableCommand.Parameters.AddWithValue("@ma_ban", tableId);
                updateTableCommand.ExecuteNonQuery();
            }

            transaction.Commit();
            return orderId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public List<MenuCategory> GetMenuCategories()
    {
        const string sql = """
            SELECT dm.ma_danh_muc, dm.ten_danh_muc,
                   m.ma_mon_an, m.ten_mon_an, m.don_gia, m.duong_dan_anh
            FROM danh_muc AS dm
            LEFT JOIN mon_an AS m
                ON m.ma_danh_muc = dm.ma_danh_muc
               AND m.trang_thai_ban = 1
            ORDER BY dm.thu_tu_hien_thi, dm.ma_danh_muc, m.ten_mon_an;
            """;

        using var connection = dbHelper.CreateConnection();
        connection.Open();
        using var command = dbHelper.CreateCommand(connection, sql);
        using var reader = command.ExecuteReader();

        var categories = new Dictionary<int, MenuCategory>();
        while (reader.Read())
        {
            var categoryId = reader.GetInt32(0);
            if (!categories.TryGetValue(categoryId, out var category))
            {
                category = new MenuCategory
                {
                    MaDanhMuc = categoryId,
                    TenDanhMuc = reader.GetString(1)
                };
                categories.Add(categoryId, category);
            }

            if (!reader.IsDBNull(2))
            {
                category.MonAn.Add(new MenuItem
                {
                    MaMonAn = reader.GetInt32(2),
                    TenMonAn = reader.GetString(3),
                    DonGia = reader.GetDecimal(4),
                    DuongDanAnh = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            }
        }

        return categories.Values.ToList();
    }

    public OrderPOSData? GetOpenOrderForTable(int tableId)
    {
        const string orderSql = """
            SELECT TOP (1) d.ma_don_hang, d.ma_ban, b.so_hieu_ban,
                   d.thoi_gian_tao, ISNULL(d.tong_tien_tam, 0)
            FROM don_hang AS d
            INNER JOIN ban_an AS b ON b.ma_ban = d.ma_ban
            WHERE d.ma_ban = @ma_ban
              AND d.trang_thai_don = @trang_thai_don
            ORDER BY d.thoi_gian_tao DESC, d.ma_don_hang DESC;
            """;

        using var connection = dbHelper.CreateConnection();
        connection.Open();
        using var orderCommand = dbHelper.CreateCommand(connection, orderSql);
        orderCommand.Parameters.AddWithValue("@ma_ban", tableId);
        orderCommand.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);

        OrderPOSData? order;
        using (var reader = orderCommand.ExecuteReader())
        {
            if (!reader.Read())
            {
                return null;
            }

            order = new OrderPOSData
            {
                MaDonHang = reader.GetInt32(0),
                MaBan = reader.GetInt32(1),
                SoHieuBan = reader.GetString(2),
                ThoiGianTao = reader.GetDateTime(3),
                TienMonAn = reader.GetDecimal(4)
            };
        }

        const string detailsSql = """
            SELECT ct.ma_chi_tiet, ct.ma_mon_an, m.ten_mon_an,
                   ct.so_luong, ct.don_gia_luc_goi, ct.ghi_chu_mon,
                   COALESCE(ct.tien_do_bep, 'CHO_NAU')
            FROM chi_tiet_don_hang AS ct
            INNER JOIN mon_an AS m ON m.ma_mon_an = ct.ma_mon_an
            WHERE ct.ma_don_hang = @ma_don_hang
            ORDER BY ct.thoi_gian_goi, ct.ma_chi_tiet;
            """;

        using var detailsCommand = dbHelper.CreateCommand(connection, detailsSql);
        detailsCommand.Parameters.AddWithValue("@ma_don_hang", order!.MaDonHang);
        using var detailsReader = detailsCommand.ExecuteReader();
        while (detailsReader.Read())
        {
            order.ChiTiet.Add(new OrderPOSLine
            {
                MaChiTiet = detailsReader.GetInt32(0),
                MaMonAn = detailsReader.GetInt32(1),
                TenMonAn = detailsReader.GetString(2),
                SoLuong = detailsReader.GetInt32(3),
                DonGia = detailsReader.GetDecimal(4),
                GhiChuMon = detailsReader.IsDBNull(5) ? null : detailsReader.GetString(5),
                TienDoBep = detailsReader.GetString(6)
            });
        }

        return order;
    }

    public List<KitchenOrderItem> GetKitchenItems()
    {
        const string sql = """
            SELECT ct.ma_chi_tiet, ct.ma_don_hang, b.so_hieu_ban, m.ten_mon_an,
                   ct.so_luong, ct.ghi_chu_mon, COALESCE(ct.tien_do_bep, 'CHO_NAU'),
                   ct.thoi_gian_goi
            FROM chi_tiet_don_hang AS ct
            INNER JOIN don_hang AS d ON d.ma_don_hang = ct.ma_don_hang
            INNER JOIN mon_an AS m ON m.ma_mon_an = ct.ma_mon_an
            LEFT JOIN ban_an AS b ON b.ma_ban = d.ma_ban
            WHERE d.trang_thai_don = @trang_thai_don
            ORDER BY ct.thoi_gian_goi, ct.ma_chi_tiet;
            """;

        using var connection = dbHelper.CreateConnection();
        connection.Open();
        using var command = dbHelper.CreateCommand(connection, sql);
        command.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
        using var reader = command.ExecuteReader();

        var items = new List<KitchenOrderItem>();
        while (reader.Read())
        {
            items.Add(new KitchenOrderItem
            {
                MaChiTiet = reader.GetInt32(0),
                MaDonHang = reader.GetInt32(1),
                SoHieuBan = reader.IsDBNull(2) ? null : reader.GetString(2),
                TenMonAn = reader.GetString(3),
                SoLuong = reader.GetInt32(4),
                GhiChuMon = reader.IsDBNull(5) ? null : reader.GetString(5),
                TienDoBep = reader.GetString(6),
                ThoiGianGoi = reader.GetDateTime(7)
            });
        }

        return items;
    }

    public bool UpdateKitchenProgress(int detailId, string progress)
    {
        const string sql = """
            UPDATE ct
            SET tien_do_bep = @tien_do_bep
            FROM chi_tiet_don_hang AS ct
            INNER JOIN don_hang AS d ON d.ma_don_hang = ct.ma_don_hang
            WHERE ct.ma_chi_tiet = @ma_chi_tiet
              AND d.trang_thai_don = @trang_thai_don;
            """;

        using var connection = dbHelper.CreateConnection();
        connection.Open();
        using var command = dbHelper.CreateCommand(connection, sql);
        command.Parameters.AddWithValue("@tien_do_bep", progress);
        command.Parameters.AddWithValue("@ma_chi_tiet", detailId);
        command.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);

        return command.ExecuteNonQuery() == 1;
    }

    public bool RemoveItem(int detailId)
    {
        using var connection = dbHelper.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            const string checkSql = """
                SELECT ct.ma_don_hang, COALESCE(ct.tien_do_bep, 'CHO_NAU'), d.trang_thai_don
                FROM chi_tiet_don_hang AS ct WITH (UPDLOCK, HOLDLOCK)
                INNER JOIN don_hang AS d WITH (UPDLOCK, HOLDLOCK) ON d.ma_don_hang = ct.ma_don_hang
                WHERE ct.ma_chi_tiet = @ma_chi_tiet;
                """;
            int orderId = 0;
            string kitchenProgress = "";
            string orderStatus = "";
            using (var cmd = dbHelper.CreateCommand(connection, checkSql, transaction))
            {
                cmd.Parameters.AddWithValue("@ma_chi_tiet", detailId);
                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    transaction.Rollback();
                    return false;
                }
                orderId = reader.GetInt32(0);
                kitchenProgress = reader.GetString(1);
                orderStatus = reader.GetString(2);
            }

            if (orderStatus != OPEN_ORDER_STATUS || kitchenProgress != "CHO_NAU")
            {
                transaction.Rollback();
                return false;
            }

            const string deleteSql = "DELETE FROM chi_tiet_don_hang WHERE ma_chi_tiet = @ma_chi_tiet;";
            using (var delCmd = dbHelper.CreateCommand(connection, deleteSql, transaction))
            {
                delCmd.Parameters.AddWithValue("@ma_chi_tiet", detailId);
                delCmd.ExecuteNonQuery();
            }

            const string updateOrderSql = """
                UPDATE don_hang
                SET tong_tien_tam = (
                    SELECT ISNULL(SUM(so_luong * don_gia_luc_goi), 0)
                    FROM chi_tiet_don_hang
                    WHERE ma_don_hang = @ma_don_hang
                )
                WHERE ma_don_hang = @ma_don_hang;
                """;
            using (var upCmd = dbHelper.CreateCommand(connection, updateOrderSql, transaction))
            {
                upCmd.Parameters.AddWithValue("@ma_don_hang", orderId);
                upCmd.ExecuteNonQuery();
            }

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public bool UpdateItemQuantity(int detailId, int newQuantity)
    {
        if (newQuantity <= 0) return RemoveItem(detailId);

        using var connection = dbHelper.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            const string checkSql = """
                SELECT ct.ma_don_hang, COALESCE(ct.tien_do_bep, 'CHO_NAU'), d.trang_thai_don
                FROM chi_tiet_don_hang AS ct WITH (UPDLOCK, HOLDLOCK)
                INNER JOIN don_hang AS d WITH (UPDLOCK, HOLDLOCK) ON d.ma_don_hang = ct.ma_don_hang
                WHERE ct.ma_chi_tiet = @ma_chi_tiet;
                """;
            int orderId = 0;
            string kitchenProgress = "";
            string orderStatus = "";
            using (var cmd = dbHelper.CreateCommand(connection, checkSql, transaction))
            {
                cmd.Parameters.AddWithValue("@ma_chi_tiet", detailId);
                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    transaction.Rollback();
                    return false;
                }
                orderId = reader.GetInt32(0);
                kitchenProgress = reader.GetString(1);
                orderStatus = reader.GetString(2);
            }

            if (orderStatus != OPEN_ORDER_STATUS || kitchenProgress != "CHO_NAU")
            {
                transaction.Rollback();
                return false;
            }

            const string updateItemSql = "UPDATE chi_tiet_don_hang SET so_luong = @so_luong WHERE ma_chi_tiet = @ma_chi_tiet;";
            using (var upItemCmd = dbHelper.CreateCommand(connection, updateItemSql, transaction))
            {
                upItemCmd.Parameters.AddWithValue("@so_luong", newQuantity);
                upItemCmd.Parameters.AddWithValue("@ma_chi_tiet", detailId);
                upItemCmd.ExecuteNonQuery();
            }

            const string updateOrderSql = """
                UPDATE don_hang
                SET tong_tien_tam = (
                    SELECT ISNULL(SUM(so_luong * don_gia_luc_goi), 0)
                    FROM chi_tiet_don_hang
                    WHERE ma_don_hang = @ma_don_hang
                )
                WHERE ma_don_hang = @ma_don_hang;
                """;
            using (var upCmd = dbHelper.CreateCommand(connection, updateOrderSql, transaction))
            {
                upCmd.Parameters.AddWithValue("@ma_don_hang", orderId);
                upCmd.ExecuteNonQuery();
            }

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
