using Microsoft.AspNetCore.Mvc;
using UshiMania.BLL;
using UshiMania.DAL;
using UshiMania.Models;

namespace UshiMania.Controllers;

[Route("api/[controller]/[action]")]
public sealed class RestaurantApiController : Controller
{
    private readonly BanAnBLL banAnBLL;
    private readonly OrderBLL orderBLL;
    private readonly HoaDonKhoBLL hoaDonKhoBLL;
    private readonly ThongKeBLL thongKeBLL;
    private readonly ILogger<RestaurantApiController> logger;

    public RestaurantApiController(IConfiguration configuration, ILogger<RestaurantApiController> logger)
    {
        var dbHelper = new DbHelper(configuration);
        banAnBLL = new BanAnBLL(new BanAnDAL(dbHelper));
        orderBLL = new OrderBLL(new OrderDAL(dbHelper));
        hoaDonKhoBLL = new HoaDonKhoBLL(new HoaDonKhoDAL(dbHelper));
        thongKeBLL = new ThongKeBLL(new ThongKeDAL(dbHelper));
        this.logger = logger;
    }

    [HttpGet]
    public IActionResult GetDanhSachBan()
    {
        try
        {
            var data = banAnBLL.GetAllTables();
            return Json(new { success = true, message = "L?y danh sách bàn thành công.", data });
        }
        catch (Exception exception)
        {
            return HandleError(exception, nameof(GetDanhSachBan));
        }
    }

    [HttpPost]
    public IActionResult CapNhatTrangThaiBan([FromBody] CapNhatTrangThaiBanRequest? request)
    {
        try
        {
            if (request is null)
            {
                return Json(new { success = false, message = "Thi?u d? li?u bàn.", data = (object?)null });
            }

            var updated = banAnBLL.ChangeTableStatus(request.MaBan, request.TrangThai);
            return Json(new
            {
                success = updated,
                message = updated ? "C?p nh?t tr?ng thái bàn thành công." : "Không tìm th?y bàn ho?c bàn ch?a th? chuy?n v? tr?ng thái yêu c?u.",
                data = updated
            });
        }
        catch (Exception exception)
        {
            return HandleError(exception, nameof(CapNhatTrangThaiBan));
        }
    }

    [HttpPost]
    public IActionResult TaoHoacCapNhatOrder([FromBody] TaoHoacCapNhatOrderRequest? request)
    {
        try
        {
            if (request is null)
            {
                return Json(new { success = false, message = "Thi?u d? li?u ??n hàng.", data = (object?)null });
            }

            var hasMenuItem = request.MaMonAn.HasValue || request.SoLuong.HasValue;
            if (hasMenuItem && (!request.MaMonAn.HasValue || !request.SoLuong.HasValue))
            {
                return Json(new { success = false, message = "C?n cung c?p c? mã món và s? l??ng.", data = (object?)null });
            }

            if (request.MaDonHang.HasValue && request.MaDonHang.Value <= 0)
            {
                return Json(new { success = false, message = "Mã ??n hàng không h?p l?.", data = (object?)null });
            }

            if (request.MaDonHang.HasValue && !hasMenuItem)
            {
                return Json(new { success = false, message = "C?n cung c?p món và s? l??ng ?? c?p nh?t ??n hàng.", data = (object?)null });
            }

            int orderId;
            var created = !request.MaDonHang.HasValue;
            if (created)
            {
                if (!request.MaBan.HasValue)
                {
                    return Json(new { success = false, message = "C?n cung c?p mã bàn ?? t?o ??n hàng.", data = (object?)null });
                }

                var newOrderId = orderBLL.CreateOrder(request.MaBan.Value, request.MaNhanVien);
                if (!newOrderId.HasValue)
                {
                    return Json(new { success = false, message = "Không th? t?o ??n hàng cho bàn này.", data = (object?)null });
                }

                orderId = newOrderId.Value;
            }
            else
            {
                orderId = request.MaDonHang.Value;
            }

            int? detailId = null;
            if (hasMenuItem)
            {
                detailId = orderBLL.AddItem(orderId, request.MaMonAn!.Value, request.SoLuong!.Value, request.GhiChuMon);
                if (!detailId.HasValue)
                {
                    return Json(new
                    {
                        success = false,
                        message = created ? "??n ?ã ???c t?o nh?ng không th? thêm món." : "Không th? thêm món vào ??n hàng.",
                        data = new { maDonHang = orderId }
                    });
                }
            }

            return Json(new
            {
                success = true,
                message = created ? "T?o ??n hàng thành công." : "C?p nh?t ??n hàng thành công.",
                data = new { maDonHang = orderId, maChiTiet = detailId }
            });
        }
        catch (Exception exception)
        {
            return HandleError(exception, nameof(TaoHoacCapNhatOrder));
        }
    }

    [HttpGet]
    public IActionResult GetDanhSachMonBep()
    {
        try
        {
            var data = orderBLL.GetKitchenItems();
            return Json(new { success = true, message = "L?y danh sách món b?p thành công.", data });
        }
        catch (Exception exception)
        {
            return HandleError(exception, nameof(GetDanhSachMonBep));
        }
    }

    [HttpPost]
    public IActionResult CapNhatTienDoBep([FromBody] CapNhatTienDoBepRequest? request)
    {
        try
        {
            if (request is null)
            {
                return Json(new { success = false, message = "Thi?u d? li?u ti?n ?? b?p.", data = (object?)null });
            }

            var updated = orderBLL.UpdateKitchenProgress(request.MaChiTiet, request.TienDoBep);
            return Json(new
            {
                success = updated,
                message = updated ? "C?p nh?t ti?n ?? b?p thành công." : "Không tìm th?y món thu?c ??n hàng ?ang m?.",
                data = updated
            });
        }
        catch (Exception exception)
        {
            return HandleError(exception, nameof(CapNhatTienDoBep));
        }
    }

    [HttpPost]
    public IActionResult ThanhToanVaTruKho([FromBody] ThanhToanVaTruKhoRequest? request)
    {
        try
        {
            if (request is null)
            {
                return Json(new { success = false, message = "Thi?u d? li?u thanh toán.", data = (object?)null });
            }

            var paid = hoaDonKhoBLL.ThanhToanVaTruKho(
                request.MaDonHang,
                request.MaThuNgan,
                request.HinhThucTra,
                request.TienGiamGia);

            return Json(new
            {
                success = paid,
                message = paid ? "Thanh toán và tr? kho thành công." : "Không th? thanh toán: ??n không h?p l?, ch?a có món ho?c t?n kho không ??.",
                data = paid
            });
        }
        catch (Exception exception)
        {
            return HandleError(exception, nameof(ThanhToanVaTruKho));
        }
    }

    [HttpGet]
    public IActionResult GetCanhBaoTonKho()
    {
        try
        {
            var data = thongKeBLL.GetCanhBaoTonKho();
            return Json(new { success = true, message = "L?y c?nh báo t?n kho thành công.", data });
        }
        catch (Exception exception)
        {
            return HandleError(exception, nameof(GetCanhBaoTonKho));
        }
    }

    [HttpGet]
    public IActionResult GetBaoCaoDoanhThu([FromQuery] DateTime? tuNgay, [FromQuery] DateTime? denNgay)
    {
        try
        {
            var data = thongKeBLL.GetBaoCaoDoanhThu(tuNgay, denNgay);
            return Json(new { success = true, message = "L?y báo cáo doanh thu thành công.", data });
        }
        catch (Exception exception)
        {
            return HandleError(exception, nameof(GetBaoCaoDoanhThu));
        }
    }

    private JsonResult HandleError(Exception exception, string actionName)
    {
        logger.LogError(exception, "Error in {ActionName}.", actionName);
        Response.StatusCode = exception is ArgumentException ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError;
        return Json(new
        {
            success = false,
            message = exception is ArgumentException ? exception.Message : "?ã x?y ra l?i khi x? lý yêu c?u.",
            data = (object?)null
        });
    }
}
