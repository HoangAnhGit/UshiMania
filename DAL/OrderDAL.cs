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
}
