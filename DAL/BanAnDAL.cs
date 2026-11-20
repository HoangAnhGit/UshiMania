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

    public bool TransferTable(int sourceTableId, int targetTableId)
    {
        using var connection = dbHelper.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            const string lockSql = """
                SELECT ma_ban, trang_thai
                FROM ban_an WITH (UPDLOCK, HOLDLOCK)
                WHERE ma_ban IN (@source_id, @target_id)
                ORDER BY ma_ban;
                """;

            var foundTableIds = new HashSet<int>();
            string? targetStatus = null;
            using (var lockCommand = dbHelper.CreateCommand(connection, lockSql, transaction))
            {
                lockCommand.Parameters.AddWithValue("@source_id", sourceTableId);
                lockCommand.Parameters.AddWithValue("@target_id", targetTableId);
                using var reader = lockCommand.ExecuteReader();
                while (reader.Read())
                {
                    var tableId = reader.GetInt32(0);
                    foundTableIds.Add(tableId);
                    if (tableId == targetTableId)
                    {
                        targetStatus = reader.IsDBNull(1) ? null : reader.GetString(1);
                    }
                }
            }

            if (foundTableIds.Count != 2 || targetStatus is "DA_DAT" or "CHO_THANH_TOAN")
            {
                transaction.Rollback();
                return false;
            }

            // Kiểm tra bàn đích xem có đơn đang mở không - Chuyển bàn chỉ chuyển sang bàn chưa có đơn
            const string checkTargetOrderSql = """
                SELECT COUNT(1)
                FROM don_hang WITH (UPDLOCK, HOLDLOCK)
                WHERE ma_ban = @target_id
                  AND trang_thai_don = @trang_thai_don;
                """;

            using (var checkCmd = dbHelper.CreateCommand(connection, checkTargetOrderSql, transaction))
            {
                checkCmd.Parameters.AddWithValue("@target_id", targetTableId);
                checkCmd.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
                if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                {
                    transaction.Rollback();
                    return false; // Bàn đích đã có đơn hàng, vui lòng dùng chức năng Gộp bàn!
                }
            }

            const string moveOrdersSql = """
                UPDATE don_hang
                SET ma_ban = @target_id
                WHERE ma_ban = @source_id
                  AND trang_thai_don = @trang_thai_don;
                """;

            int movedOrderCount;
            using (var moveCommand = dbHelper.CreateCommand(connection, moveOrdersSql, transaction))
            {
                moveCommand.Parameters.AddWithValue("@source_id", sourceTableId);
                moveCommand.Parameters.AddWithValue("@target_id", targetTableId);
                moveCommand.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
                movedOrderCount = moveCommand.ExecuteNonQuery();
            }

            if (movedOrderCount == 0)
            {
                transaction.Rollback();
                return false;
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
                updateTablesCommand.ExecuteNonQuery();
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

    public bool MergeTables(int sourceTableId, int targetTableId)
    {
        using var connection = dbHelper.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            const string lockSql = """
                SELECT ma_ban, trang_thai
                FROM ban_an WITH (UPDLOCK, HOLDLOCK)
                WHERE ma_ban IN (@source_id, @target_id)
                ORDER BY ma_ban;
                """;

            var foundTableIds = new HashSet<int>();
            string? targetStatus = null;
            using (var lockCommand = dbHelper.CreateCommand(connection, lockSql, transaction))
            {
                lockCommand.Parameters.AddWithValue("@source_id", sourceTableId);
                lockCommand.Parameters.AddWithValue("@target_id", targetTableId);
                using var reader = lockCommand.ExecuteReader();
                while (reader.Read())
                {
                    var tableId = reader.GetInt32(0);
                    foundTableIds.Add(tableId);
                    if (tableId == targetTableId)
                    {
                        targetStatus = reader.IsDBNull(1) ? null : reader.GetString(1);
                    }
                }
            }

            if (foundTableIds.Count != 2 || targetStatus is "DA_DAT" or "CHO_THANH_TOAN")
            {
                transaction.Rollback();
                return false;
            }

            // Lấy mã đơn hàng nguồn
            const string getSourceOrderSql = """
                SELECT TOP (1) ma_don_hang
                FROM don_hang WITH (UPDLOCK, HOLDLOCK)
                WHERE ma_ban = @source_id
                  AND trang_thai_don = @trang_thai_don;
                """;

            int? sourceOrderId = null;
            using (var getSourceCmd = dbHelper.CreateCommand(connection, getSourceOrderSql, transaction))
            {
                getSourceCmd.Parameters.AddWithValue("@source_id", sourceTableId);
                getSourceCmd.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
                var val = getSourceCmd.ExecuteScalar();
                if (val != null && val != DBNull.Value)
                {
                    sourceOrderId = Convert.ToInt32(val);
                }
            }

            if (sourceOrderId == null)
            {
                transaction.Rollback();
                return false;
            }

            // Lấy mã đơn hàng đích nếu có
            const string getTargetOrderSql = """
                SELECT TOP (1) ma_don_hang
                FROM don_hang WITH (UPDLOCK, HOLDLOCK)
                WHERE ma_ban = @target_id
                  AND trang_thai_don = @trang_thai_don;
                """;

            int? targetOrderId = null;
            using (var getTargetCmd = dbHelper.CreateCommand(connection, getTargetOrderSql, transaction))
            {
                getTargetCmd.Parameters.AddWithValue("@target_id", targetTableId);
                getTargetCmd.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
                var val = getTargetCmd.ExecuteScalar();
                if (val != null && val != DBNull.Value)
                {
                    targetOrderId = Convert.ToInt32(val);
                }
            }

            if (targetOrderId == null)
            {
                // Bàn đích chưa có đơn: chuyển toàn bộ đơn hàng sang bàn đích
                const string moveOrderSql = """
                    UPDATE don_hang
                    SET ma_ban = @target_id
                    WHERE ma_don_hang = @source_order_id;
                    """;
                using var moveCmd = dbHelper.CreateCommand(connection, moveOrderSql, transaction);
                moveCmd.Parameters.AddWithValue("@target_id", targetTableId);
                moveCmd.Parameters.AddWithValue("@source_order_id", sourceOrderId.Value);
                moveCmd.ExecuteNonQuery();
            }
            else
            {
                // Bàn đích đã có đơn: chuyển toàn bộ chi tiết món từ đơn nguồn sang đơn đích
                const string moveItemsSql = """
                    UPDATE chi_tiet_don_hang
                    SET ma_don_hang = @target_order_id
                    WHERE ma_don_hang = @source_order_id;
                    """;
                using (var moveItemsCmd = dbHelper.CreateCommand(connection, moveItemsSql, transaction))
                {
                    moveItemsCmd.Parameters.AddWithValue("@target_order_id", targetOrderId.Value);
                    moveItemsCmd.Parameters.AddWithValue("@source_order_id", sourceOrderId.Value);
                    moveItemsCmd.ExecuteNonQuery();
                }

                // Cập nhật lại tổng tiền tạm cho đơn đích
                const string updateTargetTotalSql = """
                    UPDATE don_hang
                    SET tong_tien_tam = (
                        SELECT ISNULL(SUM(so_luong * don_gia_luc_goi), 0)
                        FROM chi_tiet_don_hang
                        WHERE ma_don_hang = @target_order_id
                    )
                    WHERE ma_don_hang = @target_order_id;
                    """;
                using (var upTargetCmd = dbHelper.CreateCommand(connection, updateTargetTotalSql, transaction))
                {
                    upTargetCmd.Parameters.AddWithValue("@target_order_id", targetOrderId.Value);
                    upTargetCmd.ExecuteNonQuery();
                }

                // Xóa đơn hàng nguồn đã rỗng
                const string deleteSourceOrderSql = "DELETE FROM don_hang WHERE ma_don_hang = @source_order_id;";
                using (var delSourceCmd = dbHelper.CreateCommand(connection, deleteSourceOrderSql, transaction))
                {
                    delSourceCmd.Parameters.AddWithValue("@source_order_id", sourceOrderId.Value);
                    delSourceCmd.ExecuteNonQuery();
                }
            }

            // Cập nhật trạng thái 2 bàn
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
                updateTablesCommand.ExecuteNonQuery();
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

    public bool MergeOrTransfer(int sourceTableId, int targetTableId)
    {
        return MergeTables(sourceTableId, targetTableId);
    }
}
