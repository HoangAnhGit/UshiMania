using UshiMania.DAL;
using UshiMania.Models;

namespace UshiMania.BLL;

public sealed class ThongKeBLL
{
    private readonly ThongKeDAL thongKeDAL;

    public ThongKeBLL(ThongKeDAL thongKeDAL)
    {
        this.thongKeDAL = thongKeDAL;
    }

    public List<CanhBaoTonKho> GetCanhBaoTonKho()
    {
        return thongKeDAL.GetCanhBaoTonKho();
    }

    public List<BaoCaoDoanhThu> GetBaoCaoDoanhThu(DateTime? fromDate, DateTime? toDate, bool groupByHour = false)
    {
        ValidateDateRange(fromDate, toDate);
        return thongKeDAL.GetBaoCaoDoanhThu(fromDate, toDate, groupByHour);
    }

    public List<MonBanChay> GetMonBanChay(DateTime? fromDate, DateTime? toDate)
    {
        ValidateDateRange(fromDate, toDate);
        return thongKeDAL.GetMonBanChay(fromDate, toDate);
    }

    public List<LichSuTruKho> GetLichSuTruKho(DateTime? fromDate, DateTime? toDate)
    {
        ValidateDateRange(fromDate, toDate);
        return thongKeDAL.GetLichSuTruKho(fromDate, toDate);
    }

    private static void ValidateDateRange(DateTime? fromDate, DateTime? toDate)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
        {
            throw new ArgumentException("Start date must not be after end date.");
        }
    }
}
