using System.Data;
using System.Data;
using Microsoft.Data.SqlClient;
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
        const string sql = """
            UPDATE ban_an
            SET trang_thai = @trang_thai
            WHERE ma_ban = @ma_ban
              AND (@trang_thai <> N'TRONG' OR NOT EXISTS (
                  SELECT 1
                  FROM don_hang
                  WHERE don_hang.ma_ban = ban_an.ma_ban
                    AND don_hang.trang_thai_don = @trang_thai_don
              ));
            """;

        using var connection = dbHelper.CreateConnection();
        connection.Open();
        using var command = dbHelper.CreateCommand(connection, sql);
        command.Parameters.AddWithValue("@trang_thai", status);
        command.Parameters.AddWithValue("@ma_ban", tableId);
        command.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);

        return command.ExecuteNonQuery() == 1;
    }

    public bool ChuyenBan(int sourceTableId, int targetTableId)
    {
        if (sourceTableId == targetTableId)
        {
            return false;
        }

        using var connection = dbHelper.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            const string lockSql = """
                SELECT ma_ban, trang_thai
                FROM ban_an WITH (UPDLOCK, HOLDLOCK)
                WHERE ma_ban IN (@source_id, @target_id)
                ORDER BY ma_ban;
                """;

            var statuses = new Dictionary<int, string?>();
            using (var lockCommand = dbHelper.CreateCommand(connection, lockSql, transaction))
            {
                lockCommand.Parameters.AddWithValue("@source_id", sourceTableId);
                lockCommand.Parameters.AddWithValue("@target_id", targetTableId);
                using var reader = lockCommand.ExecuteReader();
                while (reader.Read())
                {
                    statuses[reader.GetInt32(0)] = reader.IsDBNull(1) ? null : reader.GetString(1);
                }
            }

            if (statuses.Count != 2 || statuses[targetTableId] != "TRONG")
            {
                transaction.Rollback();
                return false;
            }

            const string targetOrderSql = """
                SELECT COUNT(1)
                FROM don_hang WITH (UPDLOCK, HOLDLOCK)
                WHERE ma_ban = @target_id
                  AND trang_thai_don = @trang_thai_don;
                """;

            using (var targetOrderCommand = dbHelper.CreateCommand(connection, targetOrderSql, transaction))
            {
                targetOrderCommand.Parameters.AddWithValue("@target_id", targetTableId);
                targetOrderCommand.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
                if (Convert.ToInt32(targetOrderCommand.ExecuteScalar()) != 0)
                {
                    transaction.Rollback();
                    return false;
                }
            }

            const string moveOrderSql = """
                UPDATE don_hang
                SET ma_ban = @target_id
                WHERE ma_ban = @source_id
                  AND trang_thai_don = @trang_thai_don;
                """;

            using (var moveCommand = dbHelper.CreateCommand(connection, moveOrderSql, transaction))
            {
                moveCommand.Parameters.AddWithValue("@source_id", sourceTableId);
                moveCommand.Parameters.AddWithValue("@target_id", targetTableId);
                moveCommand.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
                if (moveCommand.ExecuteNonQuery() == 0)
                {
                    transaction.Rollback();
                    return false;
                }
            }

            const string updateTablesSql = """
                UPDATE ban_an
                SET trang_thai = CASE
                    WHEN ma_ban = @source_id THEN N'TRONG'
                    ELSE N'DANG_PHUC_VU'
                END
                WHERE ma_ban IN (@source_id, @target_id);
                """;

            using (var updateTablesCommand = dbHelper.CreateCommand(connection, updateTablesSql, transaction))
            {
                updateTablesCommand.Parameters.AddWithValue("@source_id", sourceTableId);
                updateTablesCommand.Parameters.AddWithValue("@target_id", targetTableId);
                if (updateTablesCommand.ExecuteNonQuery() != 2)
                {
                    transaction.Rollback();
                    return false;
                }
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

    public bool GopBan(int sourceTableId, int targetTableId)
    {
        if (sourceTableId == targetTableId)
        {
            return false;
        }

        using var connection = dbHelper.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            const string lockTablesSql = """
                SELECT ma_ban
                FROM ban_an WITH (UPDLOCK, HOLDLOCK)
                WHERE ma_ban IN (@source_id, @target_id)
                ORDER BY ma_ban;
                """;

            var foundTableIds = new HashSet<int>();
            using (var lockTablesCommand = dbHelper.CreateCommand(connection, lockTablesSql, transaction))
            {
                lockTablesCommand.Parameters.AddWithValue("@source_id", sourceTableId);
                lockTablesCommand.Parameters.AddWithValue("@target_id", targetTableId);
                using var reader = lockTablesCommand.ExecuteReader();
                while (reader.Read())
                {
                    foundTableIds.Add(reader.GetInt32(0));
                }
            }

            if (foundTableIds.Count != 2)
            {
                transaction.Rollback();
                return false;
            }

            var sourceOrderIds = GetOpenOrderIds(connection, transaction, sourceTableId);
            var targetOrderIds = GetOpenOrderIds(connection, transaction, targetTableId);
            if (sourceOrderIds.Count == 0 || targetOrderIds.Count == 0)
            {
                transaction.Rollback();
                return false;
            }

            var targetOrderId = targetOrderIds[0];

            const string consolidateDetailsSql = """
                UPDATE ct
                SET ma_don_hang = @target_order_id
                FROM chi_tiet_don_hang AS ct
                INNER JOIN don_hang AS d WITH (UPDLOCK, HOLDLOCK)
                    ON d.ma_don_hang = ct.ma_don_hang
                WHERE d.ma_ban IN (@source_id, @target_id)
                  AND d.trang_thai_don = @trang_thai_don
                  AND d.ma_don_hang <> @target_order_id;
                """;

            using (var consolidateDetailsCommand = dbHelper.CreateCommand(connection, consolidateDetailsSql, transaction))
            {
                consolidateDetailsCommand.Parameters.AddWithValue("@target_order_id", targetOrderId);
                consolidateDetailsCommand.Parameters.AddWithValue("@source_id", sourceTableId);
                consolidateDetailsCommand.Parameters.AddWithValue("@target_id", targetTableId);
                consolidateDetailsCommand.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
                consolidateDetailsCommand.ExecuteNonQuery();
            }

            const string updateTargetTotalSql = """
                UPDATE don_hang
                SET tong_tien_tam = ISNULL((
                    SELECT SUM(CAST(so_luong AS DECIMAL(18, 2)) * don_gia_luc_goi)
                    FROM chi_tiet_don_hang
                    WHERE ma_don_hang = @target_order_id
                ), 0)
                WHERE ma_don_hang = @target_order_id
                  AND trang_thai_don = @trang_thai_don;
                """;

            using (var updateTargetTotalCommand = dbHelper.CreateCommand(connection, updateTargetTotalSql, transaction))
            {
                updateTargetTotalCommand.Parameters.AddWithValue("@target_order_id", targetOrderId);
                updateTargetTotalCommand.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
                if (updateTargetTotalCommand.ExecuteNonQuery() != 1)
                {
                    transaction.Rollback();
                    return false;
                }
            }

            const string cancelSourceOrdersSql = """
                UPDATE don_hang
                SET trang_thai_don = N'DA_HUY', tong_tien_tam = 0
                WHERE ma_ban = @source_id
                  AND trang_thai_don = @trang_thai_don;
                """;

            using (var cancelSourceOrdersCommand = dbHelper.CreateCommand(connection, cancelSourceOrdersSql, transaction))
            {
                cancelSourceOrdersCommand.Parameters.AddWithValue("@source_id", sourceTableId);
                cancelSourceOrdersCommand.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
                if (cancelSourceOrdersCommand.ExecuteNonQuery() == 0)
                {
                    transaction.Rollback();
                    return false;
                }
            }

            const string cancelExtraTargetOrdersSql = """
                UPDATE don_hang
                SET trang_thai_don = N'DA_HUY', tong_tien_tam = 0
                WHERE ma_ban = @target_id
                  AND ma_don_hang <> @target_order_id
                  AND trang_thai_don = @trang_thai_don;
                """;

            using (var cancelExtraTargetOrdersCommand = dbHelper.CreateCommand(connection, cancelExtraTargetOrdersSql, transaction))
            {
                cancelExtraTargetOrdersCommand.Parameters.AddWithValue("@target_id", targetTableId);
                cancelExtraTargetOrdersCommand.Parameters.AddWithValue("@target_order_id", targetOrderId);
                cancelExtraTargetOrdersCommand.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
                cancelExtraTargetOrdersCommand.ExecuteNonQuery();
            }

            const string updateTablesSql = """
                UPDATE ban_an
                SET trang_thai = CASE
                    WHEN ma_ban = @source_id THEN N'TRONG'
                    ELSE N'DANG_PHUC_VU'
                END
                WHERE ma_ban IN (@source_id, @target_id);
                """;

            using (var updateTablesCommand = dbHelper.CreateCommand(connection, updateTablesSql, transaction))
            {
                updateTablesCommand.Parameters.AddWithValue("@source_id", sourceTableId);
                updateTablesCommand.Parameters.AddWithValue("@target_id", targetTableId);
                if (updateTablesCommand.ExecuteNonQuery() != 2)
                {
                    transaction.Rollback();
                    return false;
                }
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

    private List<int> GetOpenOrderIds(SqlConnection connection, SqlTransaction transaction, int tableId)
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
}
