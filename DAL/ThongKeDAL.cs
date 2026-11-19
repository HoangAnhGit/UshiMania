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

    public List<BaoCaoDoanhThu> GetBaoCaoDoanhThu(DateTime? fromDate, DateTime? toDate, bool groupByHour = false)
    {
        var periodExpression = groupByHour
            ? "DATEADD(hour, DATEDIFF(hour, 0, ngay_thanh_toan), 0)"
            : "CONVERT(date, ngay_thanh_toan)";
        var sql = $"""
            SELECT {periodExpression} AS ngay,
                   COUNT(*) AS so_hoa_don,
                   SUM(tien_mon_an) AS tien_mon_an,
                   SUM(tien_thue_vat) AS tien_thue_vat,
                   SUM(tong_thanh_toan) AS tong_doanh_thu
            FROM hoa_don
            WHERE (@tu_ngay IS NULL OR ngay_thanh_toan >= @tu_ngay)
              AND (@den_ngay IS NULL OR ngay_thanh_toan < DATEADD(day, 1, @den_ngay))
            GROUP BY {periodExpression}
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

    public List<MonBanChay> GetMonBanChay(DateTime? fromDate, DateTime? toDate)
    {
        const string sql = """
            SELECT m.ma_mon_an, m.ten_mon_an,
                   SUM(CONVERT(BIGINT, ct.so_luong)) AS so_luong_ban,
                   SUM(ct.so_luong * ct.don_gia_luc_goi) AS doanh_thu
            FROM hoa_don AS hd
            INNER JOIN don_hang AS d ON d.ma_don_hang = hd.ma_don_hang
            INNER JOIN chi_tiet_don_hang AS ct ON ct.ma_don_hang = d.ma_don_hang
            INNER JOIN mon_an AS m ON m.ma_mon_an = ct.ma_mon_an
            WHERE (@tu_ngay IS NULL OR hd.ngay_thanh_toan >= @tu_ngay)
              AND (@den_ngay IS NULL OR hd.ngay_thanh_toan < DATEADD(day, 1, @den_ngay))
            GROUP BY m.ma_mon_an, m.ten_mon_an
            ORDER BY so_luong_ban DESC, doanh_thu DESC;
            """;

        using var connection = dbHelper.CreateConnection();
        connection.Open();
        using var command = dbHelper.CreateCommand(connection, sql);
        command.Parameters.AddWithValue("@tu_ngay", (object?)fromDate?.Date ?? DBNull.Value);
        command.Parameters.AddWithValue("@den_ngay", (object?)toDate?.Date ?? DBNull.Value);
        using var reader = command.ExecuteReader();

        var report = new List<MonBanChay>();
        while (reader.Read())
        {
            report.Add(new MonBanChay
            {
                MaMonAn = reader.GetInt32(0),
                TenMonAn = reader.GetString(1),
                SoLuongBan = reader.GetInt64(2),
                DoanhThu = reader.GetDecimal(3)
            });
        }

        return report;
    }

    public List<LichSuTruKho> GetLichSuTruKho(DateTime? fromDate, DateTime? toDate)
    {
        const string sql = """
            SELECT nk.ma_nhat_ky, nl.ten_nguyen_lieu, nk.ma_don_hang,
                   nk.loai_bien_dong, nk.so_luong_thay_doi, nk.thoi_gian
            FROM nhat_ky_kho AS nk
            LEFT JOIN nguyen_vat_lieu AS nl ON nl.ma_nguyen_lieu = nk.ma_nguyen_lieu
            WHERE (@tu_ngay IS NULL OR nk.thoi_gian >= @tu_ngay)
              AND (@den_ngay IS NULL OR nk.thoi_gian < DATEADD(day, 1, @den_ngay))
            ORDER BY nk.thoi_gian DESC, nk.ma_nhat_ky DESC;
            """;

        using var connection = dbHelper.CreateConnection();
        connection.Open();
        using var command = dbHelper.CreateCommand(connection, sql);
        command.Parameters.AddWithValue("@tu_ngay", (object?)fromDate?.Date ?? DBNull.Value);
        command.Parameters.AddWithValue("@den_ngay", (object?)toDate?.Date ?? DBNull.Value);
        using var reader = command.ExecuteReader();

        var history = new List<LichSuTruKho>();
        while (reader.Read())
        {
            history.Add(new LichSuTruKho
            {
                MaNhatKy = reader.GetInt32(0),
                TenNguyenLieu = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                MaDonHang = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                LoaiBienDong = reader.GetString(3),
                SoLuongThayDoi = reader.GetDecimal(4),
                ThoiGian = reader.GetDateTime(5)
            });
        }

        return history;
    }
}
