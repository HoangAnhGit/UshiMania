using UshiMania.Models;

namespace UshiMania.DAL;

public sealed class ThongKeDAL
{
    private readonly DbHelper dbHelper;

    public ThongKeDAL(DbHelper dbHelper)
    {
        this.dbHelper = dbHelper;
    }

    public List<CanhBaoTonKho> GetCanhBaoTonKho()
    {
        const string sql = """
            SELECT ma_nguyen_lieu, ten_nguyen_lieu, don_vi_tinh,
                   ISNULL(so_luong_ton, 0), nguong_toi_thieu
            FROM nguyen_vat_lieu
            WHERE ISNULL(so_luong_ton, 0) <= nguong_toi_thieu
            ORDER BY ten_nguyen_lieu;
            """;

        using var connection = dbHelper.CreateConnection();
        connection.Open();
        using var command = dbHelper.CreateCommand(connection, sql);
        using var reader = command.ExecuteReader();

        var items = new List<CanhBaoTonKho>();
        while (reader.Read())
        {
            items.Add(new CanhBaoTonKho
            {
                MaNguyenLieu = reader.GetInt32(0),
                TenNguyenLieu = reader.GetString(1),
                DonViTinh = reader.GetString(2),
                SoLuongTon = reader.GetDecimal(3),
                NguongToiThieu = reader.GetDecimal(4)
            });
        }

        return items;
    }

    public List<BaoCaoDoanhThu> GetBaoCaoDoanhThu(DateTime? fromDate, DateTime? toDate)
    {
        const string sql = """
            SELECT CONVERT(date, ngay_thanh_toan) AS ngay,
                   COUNT(*) AS so_hoa_don,
                   SUM(tien_mon_an) AS tien_mon_an,
                   SUM(tien_thue_vat) AS tien_thue_vat,
                   SUM(tong_thanh_toan) AS tong_doanh_thu
            FROM hoa_don
            WHERE (@tu_ngay IS NULL OR ngay_thanh_toan >= @tu_ngay)
              AND (@den_ngay IS NULL OR ngay_thanh_toan < DATEADD(day, 1, @den_ngay))
            GROUP BY CONVERT(date, ngay_thanh_toan)
            ORDER BY ngay DESC;
            """;

        using var connection = dbHelper.CreateConnection();
        connection.Open();
        using var command = dbHelper.CreateCommand(connection, sql);
        command.Parameters.AddWithValue("@tu_ngay", (object?)fromDate?.Date ?? DBNull.Value);
        command.Parameters.AddWithValue("@den_ngay", (object?)toDate?.Date ?? DBNull.Value);
        using var reader = command.ExecuteReader();

        var report = new List<BaoCaoDoanhThu>();
        while (reader.Read())
        {
            report.Add(new BaoCaoDoanhThu
            {
                Ngay = reader.GetDateTime(0),
                SoHoaDon = reader.GetInt32(1),
                TienMonAn = reader.GetDecimal(2),
                TienThueVat = reader.GetDecimal(3),
                TongDoanhThu = reader.GetDecimal(4)
            });
        }

        return report;
    }
}
