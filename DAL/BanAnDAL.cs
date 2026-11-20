using System.Data;
using UshiMania.Models;

namespace UshiMania.DAL;

public sealed class BanAnDAL
{
    private const string OPEN_ORDER_STATUS = "DANG_MO";
    private readonly DbHelper dbHelper;

    public BanAnDAL(DbHelper dbHelper)
    {
        this.dbHelper = dbHelper;
    }

    public List<BanAn> GetAll()
    {
        const string sql = """
            SELECT b.ma_ban, b.so_hieu_ban, b.khu_vuc, b.suc_chua, b.trang_thai,
                   ISNULL(active_orders.tong_tien_tam, 0) AS tong_tien_tam
            FROM ban_an AS b
            OUTER APPLY (
                SELECT SUM(ISNULL(d.tong_tien_tam, 0)) AS tong_tien_tam
                FROM don_hang AS d
                WHERE d.ma_ban = b.ma_ban
                  AND d.trang_thai_don = N'DANG_MO'
            ) AS active_orders
            ORDER BY b.khu_vuc, b.so_hieu_ban;
            """;

        using var connection = dbHelper.CreateConnection();
        connection.Open();
        using var command = dbHelper.CreateCommand(connection, sql);
        using var reader = command.ExecuteReader();

        var tables = new List<BanAn>();
        while (reader.Read())
        {
            tables.Add(new BanAn
            {
                MaBan = reader.GetInt32(0),
                SoHieuBan = reader.GetString(1),
                KhuVuc = reader.GetString(2),
                SucChua = reader.GetInt32(3),
                TrangThai = reader.IsDBNull(4) ? null : reader.GetString(4),
                TongTienTamTinh = reader.GetDecimal(5)
            });
        }

        return tables;
    }

    public bool ChangeStatus(int tableId, string status)
    {
        using var connection = dbHelper.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            const string lockTableSql = """
                SELECT ma_ban
                FROM ban_an WITH (UPDLOCK, HOLDLOCK)
                WHERE ma_ban = @ma_ban;
                """;

            using (var lockTableCommand = dbHelper.CreateCommand(connection, lockTableSql, transaction))
            {
                lockTableCommand.Parameters.AddWithValue("@ma_ban", tableId);
                if (lockTableCommand.ExecuteScalar() is null)
                {
                    transaction.Rollback();
                    return false;
                }
            }

            const string hasOpenOrderSql = """
                SELECT COUNT(1)
                FROM don_hang WITH (UPDLOCK, HOLDLOCK)
                WHERE ma_ban = @ma_ban
                  AND trang_thai_don = @trang_thai_don;
                """;

            bool hasOpenOrder;
            using (var hasOpenOrderCommand = dbHelper.CreateCommand(connection, hasOpenOrderSql, transaction))
            {
                hasOpenOrderCommand.Parameters.AddWithValue("@ma_ban", tableId);
                hasOpenOrderCommand.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
                hasOpenOrder = Convert.ToInt32(hasOpenOrderCommand.ExecuteScalar()) > 0;
            }

            var validTransition = hasOpenOrder
                ? status is "DANG_PHUC_VU" or "CHO_THANH_TOAN"
                : status is "TRONG" or "DA_DAT";
            if (!validTransition)
            {
                transaction.Rollback();
                return false;
            }

            const string updateSql = """
                UPDATE ban_an
                SET trang_thai = @trang_thai
                WHERE ma_ban = @ma_ban;
                """;

            using var updateCommand = dbHelper.CreateCommand(connection, updateSql, transaction);
            updateCommand.Parameters.AddWithValue("@trang_thai", status);
            updateCommand.Parameters.AddWithValue("@ma_ban", tableId);
            var updated = updateCommand.ExecuteNonQuery() == 1;
            transaction.Commit();
            return updated;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

    }

    public bool ChuyenBan(int maBanNguon, int maBanDich)
    {
        if (maBanNguon == maBanDich)
        {
            return false;
        }

        using var connection = dbHelper.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            var tableStatuses = GetLockedTableStatuses(connection, transaction, maBanNguon, maBanDich);
            if (tableStatuses.Count != 2 || tableStatuses[maBanDich] != "TRONG")
            {
                transaction.Rollback();
                return false;
            }

            var sourceOrders = GetOpenOrderIds(connection, transaction, maBanNguon);
            if (sourceOrders.Count == 0 || GetOpenOrderIds(connection, transaction, maBanDich).Count > 0)
            {
                transaction.Rollback();
                return false;
            }

            const string moveOrderSql = """
                UPDATE don_hang
                SET ma_ban = @ma_ban_dich
                WHERE ma_ban = @ma_ban_nguon
                  AND trang_thai_don = @trang_thai_don;
                """;

            using (var moveOrderCommand = dbHelper.CreateCommand(connection, moveOrderSql, transaction))
            {
                moveOrderCommand.Parameters.AddWithValue("@ma_ban_dich", maBanDich);
                moveOrderCommand.Parameters.AddWithValue("@ma_ban_nguon", maBanNguon);
                moveOrderCommand.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
                if (moveOrderCommand.ExecuteNonQuery() != sourceOrders.Count)
                {
                    transaction.Rollback();
                    return false;
                }
            }

            if (!UpdateTableStatuses(connection, transaction, maBanNguon, maBanDich))
            {
                transaction.Rollback();
                return false;
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

    public bool GopBan(int maBanNguon, int maBanDich)
    {
        if (maBanNguon == maBanDich)
        {
            return false;
        }

        using var connection = dbHelper.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            var tableStatuses = GetLockedTableStatuses(connection, transaction, maBanNguon, maBanDich);
            if (tableStatuses.Count != 2)
            {
                transaction.Rollback();
                return false;
            }

            var sourceOrderIds = GetOpenOrderIds(connection, transaction, maBanNguon);
            var targetOrderIds = GetOpenOrderIds(connection, transaction, maBanDich);
            if (sourceOrderIds.Count == 0 || targetOrderIds.Count == 0)
            {
                transaction.Rollback();
                return false;
            }

            var targetOrderId = targetOrderIds[0];
            const string moveDetailsSql = """
                UPDATE ct
                SET ma_don_hang = @ma_don_dich
                FROM chi_tiet_don_hang AS ct
                INNER JOIN don_hang AS d WITH (UPDLOCK, HOLDLOCK)
                    ON d.ma_don_hang = ct.ma_don_hang
                WHERE d.ma_ban IN (@ma_ban_nguon, @ma_ban_dich)
                  AND d.trang_thai_don = @trang_thai_don
                  AND d.ma_don_hang <> @ma_don_dich;
                """;

            using (var moveDetailsCommand = dbHelper.CreateCommand(connection, moveDetailsSql, transaction))
            {
                moveDetailsCommand.Parameters.AddWithValue("@ma_don_dich", targetOrderId);
                moveDetailsCommand.Parameters.AddWithValue("@ma_ban_nguon", maBanNguon);
                moveDetailsCommand.Parameters.AddWithValue("@ma_ban_dich", maBanDich);
                moveDetailsCommand.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
                moveDetailsCommand.ExecuteNonQuery();
            }

            const string recalculateTargetSql = """
                UPDATE don_hang
                SET tong_tien_tam = ISNULL((
                    SELECT SUM(CAST(so_luong AS DECIMAL(18, 2)) * don_gia_luc_goi)
                    FROM chi_tiet_don_hang
                    WHERE ma_don_hang = @ma_don_dich
                ), 0)
                WHERE ma_don_hang = @ma_don_dich
                  AND trang_thai_don = @trang_thai_don;
                """;

            using (var recalculateTargetCommand = dbHelper.CreateCommand(connection, recalculateTargetSql, transaction))
            {
                recalculateTargetCommand.Parameters.AddWithValue("@ma_don_dich", targetOrderId);
                recalculateTargetCommand.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
                if (recalculateTargetCommand.ExecuteNonQuery() != 1)
                {
                    transaction.Rollback();
                    return false;
                }
            }

            const string deleteEmptySourceOrdersSql = """
                DELETE FROM don_hang
                WHERE ma_ban = @ma_ban_nguon
                  AND trang_thai_don = @trang_thai_don
                  AND NOT EXISTS (
                      SELECT 1
                      FROM chi_tiet_don_hang
                      WHERE chi_tiet_don_hang.ma_don_hang = don_hang.ma_don_hang
                  );
                """;

            using (var deleteSourceOrdersCommand = dbHelper.CreateCommand(connection, deleteEmptySourceOrdersSql, transaction))
            {
                deleteSourceOrdersCommand.Parameters.AddWithValue("@ma_ban_nguon", maBanNguon);
                deleteSourceOrdersCommand.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
                if (deleteSourceOrdersCommand.ExecuteNonQuery() != sourceOrderIds.Count)
                {
                    transaction.Rollback();
                    return false;
                }
            }

            const string deleteExtraTargetOrdersSql = """
                DELETE FROM don_hang
                WHERE ma_ban = @ma_ban_dich
                  AND ma_don_hang <> @ma_don_dich
                  AND trang_thai_don = @trang_thai_don
                  AND NOT EXISTS (
                      SELECT 1
                      FROM chi_tiet_don_hang
                      WHERE chi_tiet_don_hang.ma_don_hang = don_hang.ma_don_hang
                  );
                """;

            using (var deleteExtraTargetOrdersCommand = dbHelper.CreateCommand(connection, deleteExtraTargetOrdersSql, transaction))
            {
                deleteExtraTargetOrdersCommand.Parameters.AddWithValue("@ma_ban_dich", maBanDich);
                deleteExtraTargetOrdersCommand.Parameters.AddWithValue("@ma_don_dich", targetOrderId);
                deleteExtraTargetOrdersCommand.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
                if (deleteExtraTargetOrdersCommand.ExecuteNonQuery() != targetOrderIds.Count - 1)
                {
                    transaction.Rollback();
                    return false;
                }
            }

            if (!UpdateTableStatuses(connection, transaction, maBanNguon, maBanDich))
            {
                transaction.Rollback();
                return false;
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

    private Dictionary<int, string?> GetLockedTableStatuses(
        Microsoft.Data.SqlClient.SqlConnection connection,
        Microsoft.Data.SqlClient.SqlTransaction transaction,
        int maBanNguon,
        int maBanDich)
    {
        const string sql = """
                SELECT ma_ban, trang_thai
                FROM ban_an WITH (UPDLOCK, HOLDLOCK)
                WHERE ma_ban IN (@ma_ban_nguon, @ma_ban_dich)
                ORDER BY ma_ban;
                """;

        using var command = dbHelper.CreateCommand(connection, sql, transaction);
        command.Parameters.AddWithValue("@ma_ban_nguon", maBanNguon);
        command.Parameters.AddWithValue("@ma_ban_dich", maBanDich);
        using var reader = command.ExecuteReader();
        var statuses = new Dictionary<int, string?>();
        while (reader.Read())
        {
            statuses[reader.GetInt32(0)] = reader.IsDBNull(1) ? null : reader.GetString(1);
        }

        return statuses;
    }

    private List<int> GetOpenOrderIds(
        Microsoft.Data.SqlClient.SqlConnection connection,
        Microsoft.Data.SqlClient.SqlTransaction transaction,
        int tableId)
    {
        const string sql = """
            SELECT ma_don_hang
            FROM don_hang WITH (UPDLOCK, HOLDLOCK)
            WHERE ma_ban = @ma_ban
              AND trang_thai_don = @trang_thai_don
            ORDER BY thoi_gian_tao, ma_don_hang;
            """;

        using var command = dbHelper.CreateCommand(connection, sql, transaction);
        command.Parameters.AddWithValue("@ma_ban", tableId);
        command.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
        using var reader = command.ExecuteReader();
        var orderIds = new List<int>();
        while (reader.Read())
        {
            orderIds.Add(reader.GetInt32(0));
        }

        return orderIds;
    }

    private bool UpdateTableStatuses(
        Microsoft.Data.SqlClient.SqlConnection connection,
        Microsoft.Data.SqlClient.SqlTransaction transaction,
        int maBanNguon,
        int maBanDich)
    {
        const string sql = """
            UPDATE ban_an
            SET trang_thai = CASE
                WHEN ma_ban = @ma_ban_nguon THEN N'TRONG'
                ELSE N'DANG_PHUC_VU'
            END
            WHERE ma_ban IN (@ma_ban_nguon, @ma_ban_dich);
            """;

        using var command = dbHelper.CreateCommand(connection, sql, transaction);
        command.Parameters.AddWithValue("@ma_ban_nguon", maBanNguon);
        command.Parameters.AddWithValue("@ma_ban_dich", maBanDich);
        return command.ExecuteNonQuery() == 2;
    }
}
