using System.Data;
using System.Data;
using Microsoft.Data.SqlClient;

namespace UshiMania.DAL;

public sealed class HoaDonKhoDAL
{
    private const string OPEN_ORDER_STATUS = "DANG_MO";
    private const string PAID_ORDER_STATUS = "DA_THANH_TOAN";
    private const string INVENTORY_CHANGE_TYPE = "XUAT_KHO";
    private const decimal VAT_RATE = 0.08m;

    private readonly DbHelper dbHelper;

    public HoaDonKhoDAL(DbHelper dbHelper)
    {
        this.dbHelper = dbHelper;
    }

    public bool ThanhToanVaTruKho(
        int orderId,
        int? cashierId,
        string paymentMethod,
        decimal discount)
    {
        using var connection = dbHelper.CreateConnection();
        connection.Open();

        var unfinishedCount = CountUnfinishedKitchenItems(connection, null, orderId);
        if (unfinishedCount > 0)
        {
            throw new UnfinishedKitchenItemsException(unfinishedCount);
        }

        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            const string lockOrderSql = """
                SELECT ma_ban
                FROM don_hang WITH (UPDLOCK, HOLDLOCK)
                WHERE ma_don_hang = @ma_don_hang
                  AND trang_thai_don = @trang_thai_don;
                """;

            object? tableIdValue;
            using (var lockOrderCommand = dbHelper.CreateCommand(connection, lockOrderSql, transaction))
            {
                lockOrderCommand.Parameters.AddWithValue("@ma_don_hang", orderId);
                lockOrderCommand.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
                tableIdValue = lockOrderCommand.ExecuteScalar();
            }

            if (tableIdValue is null or DBNull)
            {
                transaction.Rollback();
                return false;
            }

            var tableId = Convert.ToInt32(tableIdValue);

            unfinishedCount = CountUnfinishedKitchenItems(connection, transaction, orderId);
            if (unfinishedCount > 0)
            {
                throw new UnfinishedKitchenItemsException(unfinishedCount);
            }

            const string subtotalSql = """
                SELECT SUM(CAST(so_luong AS DECIMAL(18, 2)) * don_gia_luc_goi)
                FROM chi_tiet_don_hang WITH (UPDLOCK, HOLDLOCK)
                WHERE ma_don_hang = @ma_don_hang;
                """;

            decimal subtotal;
            using (var subtotalCommand = dbHelper.CreateCommand(connection, subtotalSql, transaction))
            {
                subtotalCommand.Parameters.AddWithValue("@ma_don_hang", orderId);
                var result = subtotalCommand.ExecuteScalar();
                if (result is null or DBNull)
                {
                    transaction.Rollback();
                    return false;
                }

                subtotal = Convert.ToDecimal(result);
            }

            var vat = decimal.Round(subtotal * VAT_RATE, 2, MidpointRounding.AwayFromZero);
            var total = subtotal + vat - discount;
            if (total < 0)
            {
                transaction.Rollback();
                return false;
            }

            var ingredients = GetIngredientRequirements(connection, transaction, orderId);
            foreach (var ingredient in ingredients)
            {
                if (!DeductIngredient(connection, transaction, orderId, ingredient))
                {
                    transaction.Rollback();
                    return false;
                }
            }

            const string insertInvoiceSql = """
                INSERT INTO hoa_don
                    (ma_don_hang, ma_thu_ngan, tien_mon_an, tien_thue_vat,
                     tien_giam_gia, tong_thanh_toan, hinh_thuc_tra)
                VALUES
                    (@ma_don_hang, @ma_thu_ngan, @tien_mon_an, @tien_thue_vat,
                     @tien_giam_gia, @tong_thanh_toan, @hinh_thuc_tra);
                """;

            using (var invoiceCommand = dbHelper.CreateCommand(connection, insertInvoiceSql, transaction))
            {
                invoiceCommand.Parameters.AddWithValue("@ma_don_hang", orderId);
                invoiceCommand.Parameters.AddWithValue("@ma_thu_ngan", (object?)cashierId ?? DBNull.Value);
                invoiceCommand.Parameters.AddWithValue("@tien_mon_an", subtotal);
                invoiceCommand.Parameters.AddWithValue("@tien_thue_vat", vat);
                invoiceCommand.Parameters.AddWithValue("@tien_giam_gia", discount);
                invoiceCommand.Parameters.AddWithValue("@tong_thanh_toan", total);
                invoiceCommand.Parameters.AddWithValue("@hinh_thuc_tra", paymentMethod);
                invoiceCommand.ExecuteNonQuery();
            }

            const string closeOrderSql = """
                UPDATE don_hang
                SET trang_thai_don = @trang_thai_da_thanh_toan
                WHERE ma_don_hang = @ma_don_hang
                  AND trang_thai_don = @trang_thai_don;
                """;

            using (var closeOrderCommand = dbHelper.CreateCommand(connection, closeOrderSql, transaction))
            {
                closeOrderCommand.Parameters.AddWithValue("@trang_thai_da_thanh_toan", PAID_ORDER_STATUS);
                closeOrderCommand.Parameters.AddWithValue("@ma_don_hang", orderId);
                closeOrderCommand.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
                if (closeOrderCommand.ExecuteNonQuery() != 1)
                {
                    transaction.Rollback();
                    return false;
                }
            }

            const string releaseTableSql = """
                UPDATE ban_an
                SET trang_thai = CASE
                    WHEN EXISTS (
                        SELECT 1
                        FROM don_hang
                        WHERE don_hang.ma_ban = ban_an.ma_ban
                          AND don_hang.trang_thai_don = @trang_thai_don
                    ) THEN N'DANG_PHUC_VU'
                    ELSE N'TRONG'
                END
                WHERE ma_ban = @ma_ban;
                """;

            using (var releaseTableCommand = dbHelper.CreateCommand(connection, releaseTableSql, transaction))
            {
                releaseTableCommand.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
                releaseTableCommand.Parameters.AddWithValue("@ma_ban", tableId);
                releaseTableCommand.ExecuteNonQuery();
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

    private int CountUnfinishedKitchenItems(SqlConnection connection, SqlTransaction? transaction, int orderId)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM chi_tiet_don_hang AS ct WITH (UPDLOCK, HOLDLOCK)
            INNER JOIN don_hang AS d WITH (UPDLOCK, HOLDLOCK)
                ON d.ma_don_hang = ct.ma_don_hang
            WHERE ct.ma_don_hang = @ma_don_hang
              AND d.trang_thai_don = @trang_thai_don
              AND ISNULL(ct.tien_do_bep, 'CHO_NAU') <> 'DA_XONG';
            """;

        using var command = dbHelper.CreateCommand(connection, sql, transaction);
        command.Parameters.AddWithValue("@ma_don_hang", orderId);
        command.Parameters.AddWithValue("@trang_thai_don", OPEN_ORDER_STATUS);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private List<IngredientRequirement> GetIngredientRequirements(
        SqlConnection connection,
        SqlTransaction transaction,
        int orderId)
    {
        const string sql = """
            SELECT c.ma_nguyen_lieu, SUM(c.dinh_luong * ct.so_luong) AS so_luong_can
            FROM chi_tiet_don_hang AS ct WITH (UPDLOCK, HOLDLOCK)
            INNER JOIN cong_thuc AS c ON c.ma_mon_an = ct.ma_mon_an
            WHERE ct.ma_don_hang = @ma_don_hang
              AND c.ma_nguyen_lieu IS NOT NULL
            GROUP BY c.ma_nguyen_lieu;
            """;

        using var command = dbHelper.CreateCommand(connection, sql, transaction);
        command.Parameters.AddWithValue("@ma_don_hang", orderId);
        using var reader = command.ExecuteReader();

        var requirements = new List<IngredientRequirement>();
        while (reader.Read())
        {
            requirements.Add(new IngredientRequirement(reader.GetInt32(0), reader.GetDecimal(1)));
        }

        return requirements;
    }

    private bool DeductIngredient(
        SqlConnection connection,
        SqlTransaction transaction,
        int orderId,
        IngredientRequirement ingredient)
    {
        const string updateSql = """
            UPDATE nguyen_vat_lieu
            SET so_luong_ton = ISNULL(so_luong_ton, 0) - @so_luong_can
            WHERE ma_nguyen_lieu = @ma_nguyen_lieu
              AND ISNULL(so_luong_ton, 0) >= @so_luong_can;
            """;

        using (var updateCommand = dbHelper.CreateCommand(connection, updateSql, transaction))
        {
            updateCommand.Parameters.AddWithValue("@so_luong_can", ingredient.Quantity);
            updateCommand.Parameters.AddWithValue("@ma_nguyen_lieu", ingredient.IngredientId);
            if (updateCommand.ExecuteNonQuery() != 1)
            {
                return false;
            }
        }

        const string insertLogSql = """
            INSERT INTO nhat_ky_kho
                (ma_nguyen_lieu, ma_don_hang, loai_bien_dong, so_luong_thay_doi)
            VALUES
                (@ma_nguyen_lieu, @ma_don_hang, @loai_bien_dong, @so_luong_thay_doi);
            """;

        using var logCommand = dbHelper.CreateCommand(connection, insertLogSql, transaction);
        logCommand.Parameters.AddWithValue("@ma_nguyen_lieu", ingredient.IngredientId);
        logCommand.Parameters.AddWithValue("@ma_don_hang", orderId);
        logCommand.Parameters.AddWithValue("@loai_bien_dong", INVENTORY_CHANGE_TYPE);
        logCommand.Parameters.AddWithValue("@so_luong_thay_doi", -ingredient.Quantity);
        logCommand.ExecuteNonQuery();
        return true;
    }

    private sealed record IngredientRequirement(int IngredientId, decimal Quantity);
}

public sealed class UnfinishedKitchenItemsException(int unfinishedCount)
    : InvalidOperationException($"Kh\u00f4ng th\u1ec3 thanh to\u00e1n! V\u1eabn c\u00f2n {unfinishedCount} m\u00f3n ch\u01b0a ch\u1ebf bi\u1ebfn xong t\u1ea1i b\u1ebfp.");
