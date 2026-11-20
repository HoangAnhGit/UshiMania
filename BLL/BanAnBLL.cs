using UshiMania.DAL;
using UshiMania.Models;

namespace UshiMania.BLL;

public sealed class BanAnBLL
{
    private static readonly HashSet<string> VALID_STATUSES = new(StringComparer.Ordinal)
    {
        "TRONG",
        "DANG_PHUC_VU",
        "CHO_THANH_TOAN",
        "DA_DAT"
    };

    private readonly BanAnDAL banAnDAL;

    public BanAnBLL(BanAnDAL banAnDAL)
    {
        this.banAnDAL = banAnDAL;
    }

    public List<BanAn> GetAllTables()
    {
        return banAnDAL.GetAll();
    }

    public bool ChangeTableStatus(int tableId, string status)
    {
        if (tableId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tableId), "Mã bàn phải là số dương.");
        }

        if (string.IsNullOrWhiteSpace(status) || !VALID_STATUSES.Contains(status))
        {
            throw new ArgumentException("Trạng thái bàn không được hỗ trợ.", nameof(status));
        }

        return banAnDAL.ChangeStatus(tableId, status);
    }

    public bool MergeOrTransferTables(int sourceTableId, int targetTableId)
    {
        if (sourceTableId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceTableId), "Mã bàn phải là số dương.");
        }

        if (targetTableId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetTableId), "Mã bàn phải là số dương.");
        }

        if (sourceTableId == targetTableId)
        {
            throw new ArgumentException("Bàn nguồn và bàn đích phải khác nhau.");
        }

        return banAnDAL.MergeOrTransfer(sourceTableId, targetTableId);
    }
}
