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
            throw new ArgumentOutOfRangeException(nameof(tableId), "Table ID must be positive.");
        }

        if (string.IsNullOrWhiteSpace(status) || !VALID_STATUSES.Contains(status))
        {
            throw new ArgumentException("Unsupported table status.", nameof(status));
        }

        return banAnDAL.ChangeStatus(tableId, status);
    }

    public bool ChuyenBan(int sourceTableId, int targetTableId)
    {
        if (sourceTableId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceTableId), "Table ID must be positive.");
        }

        if (targetTableId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetTableId), "Table ID must be positive.");
        }

        if (sourceTableId == targetTableId)
        {
            throw new ArgumentException("Source and target tables must be different.");
        }

        return banAnDAL.ChuyenBan(sourceTableId, targetTableId);
    }

    public bool GopBan(int sourceTableId, int targetTableId)
    {
        if (sourceTableId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceTableId), "Table ID must be positive.");
        }

        if (targetTableId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetTableId), "Table ID must be positive.");
        }

        if (sourceTableId == targetTableId)
        {
            throw new ArgumentException("Source and target tables must be different.");
        }

        return banAnDAL.GopBan(sourceTableId, targetTableId);
    }
}
