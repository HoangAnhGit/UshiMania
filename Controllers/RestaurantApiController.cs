using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using UshiMania.BLL;
using UshiMania.DAL;
using UshiMania.Hubs;
using UshiMania.Models;

namespace UshiMania.Controllers;

[ApiController]
[Route("api/restaurant")]
public sealed class RestaurantApiController : Controller
{
    private readonly BanAnBLL banAnBLL;
    private readonly OrderBLL orderBLL;
    private readonly HoaDonKhoBLL hoaDonKhoBLL;
    private readonly ThongKeBLL thongKeBLL;
    private readonly IHubContext<RestaurantHub> restaurantHub;
    private readonly ILogger<RestaurantApiController> logger;

    public RestaurantApiController(
        IConfiguration configuration,
        IHubContext<RestaurantHub> restaurantHub,
        ILogger<RestaurantApiController> logger)
    {
        var dbHelper = new DbHelper(configuration);
        banAnBLL = new BanAnBLL(new BanAnDAL(dbHelper));
        orderBLL = new OrderBLL(new OrderDAL(dbHelper));
        hoaDonKhoBLL = new HoaDonKhoBLL(new HoaDonKhoDAL(dbHelper));
        thongKeBLL = new ThongKeBLL(new ThongKeDAL(dbHelper));
        this.restaurantHub = restaurantHub;
        this.logger = logger;
    }

    [HttpGet("tables")]
    public IActionResult GetDanhSachBan()
    {
        try
        {
            var data = banAnBLL.GetAllTables();
            return Json(new { success = true, message = "Lấy danh sách bàn thành công.", data });
        }
        catch (Exception exception)
        {
            return HandleError(exception, nameof(GetDanhSachBan));
        }
    }

    [HttpPut("tables/status")]
    public async Task<IActionResult> CapNhatTrangThaiBan([FromBody] CapNhatTrangThaiBanRequest? request)
    {
        try
        {
            if (request is null)
            {
                return Json(new { success = false, message = "Thiếu dữ liệu bàn.", data = (object?)null });
            }

            var updated = banAnBLL.ChangeTableStatus(request.MaBan, request.TrangThai);
            if (updated)
            {
                await restaurantHub.Clients.All.SendAsync("ReceiveDataUpdate", new
                {
                    type = "table-status",
                    tableId = request.MaBan
                });
            }

            return Json(new
            {
                success = updated,
                message = updated ? "Cập nhật trạng thái bàn thành công." : "Không tìm thấy bàn hoặc bàn chưa thể chuyển về trạng thái yêu cầu.",
                data = updated
            });
        }
        catch (Exception exception)
        {
            return HandleError(exception, nameof(CapNhatTrangThaiBan));
        }
    }

    [HttpPost("tables/transfer")]
    public async Task<IActionResult> ChuyenBan([FromBody] ChuyenBanRequest? request)
    {
        try
        {
            if (request is null || request.MaBanNguon <= 0 || request.MaBanDich <= 0 || request.MaBanNguon == request.MaBanDich)
            {
                return Json(new { success = false, message = "Bàn chuyển và bàn nhận không hợp lệ.", data = (object?)null });
            }

            var result = banAnBLL.TransferTable(request.MaBanNguon, request.MaBanDich);
            if (result)
            {
                await restaurantHub.Clients.All.SendAsync("ReceiveDataUpdate", new
                {
                    type = "table-status",
                    sourceTableId = request.MaBanNguon,
                    targetTableId = request.MaBanDich
                });
                await restaurantHub.Clients.All.SendAsync("ReceiveDataUpdate", new
                {
                    type = "order-updated"
                });
            }

            return Json(new
            {
                success = result,
                message = result ? "Chuyển bàn thành công." : "Không thể chuyển bàn. Bàn nguồn chưa có đơn hoặc bàn đích đã có người ngồi / đã đặt.",
                data = result
            });
        }
        catch (Exception exception)
        {
            return HandleError(exception, nameof(ChuyenBan));
        }
    }

    [HttpPost("tables/merge")]
    public async Task<IActionResult> GopBan([FromBody] GopBanRequest? request)
    {
        try
        {
            if (request is null || request.MaBanNguon <= 0 || request.MaBanDich <= 0 || request.MaBanNguon == request.MaBanDich)
            {
                return Json(new { success = false, message = "Bàn gộp và bàn đích không hợp lệ.", data = (object?)null });
            }

            var result = banAnBLL.MergeTables(request.MaBanNguon, request.MaBanDich);
            if (result)
            {
                await restaurantHub.Clients.All.SendAsync("ReceiveDataUpdate", new
                {
                    type = "table-status",
                    sourceTableId = request.MaBanNguon,
                    targetTableId = request.MaBanDich
                });
                await restaurantHub.Clients.All.SendAsync("ReceiveDataUpdate", new
                {
                    type = "order-updated"
                });
            }

            return Json(new
            {
                success = result,
                message = result ? "Gộp bàn thành công." : "Không thể gộp bàn. Vui lòng kiểm tra lại đơn hàng của hai bàn.",
                data = result
            });
        }
        catch (Exception exception)
        {
            return HandleError(exception, nameof(GopBan));
        }
    }

    [HttpDelete("orders/items/{maChiTiet:int}")]
    public async Task<IActionResult> XoaMonKhoiOrder([FromRoute] int maChiTiet)
    {
        try
        {
            var result = orderBLL.RemoveItem(maChiTiet);
            if (result)
            {
                await restaurantHub.Clients.All.SendAsync("ReceiveDataUpdate", new
                {
                    type = "order-updated",
                    detailId = maChiTiet
                });
                await restaurantHub.Clients.All.SendAsync("ReceiveDataUpdate", new
                {
                    type = "kitchen-progress",
                    detailId = maChiTiet
                });
            }

            return Json(new
            {
                success = result,
                message = result ? "Đã xóa món khỏi đơn hàng." : "Không thể xóa: món đã được bếp chế biến hoặc đơn không còn mở.",
                data = result
            });
        }
        catch (Exception exception)
        {
            return HandleError(exception, nameof(XoaMonKhoiOrder));
        }
    }

    [HttpPut("orders/items/{maChiTiet:int}/quantity")]
    public async Task<IActionResult> CapNhatSoLuongMon([FromRoute] int maChiTiet, [FromBody] CapNhatSoLuongChiTietRequest? request)
    {
        try
        {
            if (request is null)
            {
                return Json(new { success = false, message = "Thiếu dữ liệu số lượng.", data = (object?)null });
            }

            var result = orderBLL.UpdateItemQuantity(maChiTiet, request.SoLuong);
            if (result)
            {
                await restaurantHub.Clients.All.SendAsync("ReceiveDataUpdate", new
                {
                    type = "order-updated",
                    detailId = maChiTiet
                });
                await restaurantHub.Clients.All.SendAsync("ReceiveDataUpdate", new
                {
                    type = "kitchen-progress",
                    detailId = maChiTiet
                });
            }

            return Json(new
            {
                success = result,
                message = result ? "Cập nhật số lượng món thành công." : "Không thể cập nhật: món đã được bếp chế biến hoặc đơn không còn mở.",
                data = result
            });
        }
        catch (Exception exception)
        {
            return HandleError(exception, nameof(CapNhatSoLuongMon));
        }
    }

    [HttpPost("orders")]
    public async Task<IActionResult> TaoHoacCapNhatOrder([FromBody] TaoHoacCapNhatOrderRequest? request)
    {
        try
        {
            if (request is null)
            {
                return Json(new { success = false, message = "Thiếu dữ liệu đơn hàng.", data = (object?)null });
            }

            var hasMenuItem = request.MaMonAn.HasValue || request.SoLuong.HasValue;
            if (hasMenuItem && (!request.MaMonAn.HasValue || !request.SoLuong.HasValue))
            {
                return Json(new { success = false, message = "Cần cung cấp cả mã món và số lượng.", data = (object?)null });
            }

            if (request.MaDonHang.HasValue && request.MaDonHang.Value <= 0)
            {
                return Json(new { success = false, message = "Mã đơn hàng không hợp lệ.", data = (object?)null });
            }

            if (request.MaDonHang.HasValue && !hasMenuItem)
            {
                return Json(new { success = false, message = "Cần cung cấp món và số lượng để cập nhật đơn hàng.", data = (object?)null });
            }

            int orderId;
            var created = !request.MaDonHang.HasValue;
            if (created)
            {
                if (!request.MaBan.HasValue)
                {
                    return Json(new { success = false, message = "Cần cung cấp mã bàn để tạo đơn hàng.", data = (object?)null });
                }

                var newOrderId = orderBLL.CreateOrder(request.MaBan.Value, request.MaNhanVien);
                if (!newOrderId.HasValue)
                {
                    return Json(new { success = false, message = "Không thể tạo đơn hàng cho bàn này.", data = (object?)null });
                }

                orderId = newOrderId.Value;
            }
            else
            {
                orderId = request.MaDonHang!.Value;
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
                        message = created ? "Đơn đã được tạo nhưng không thể thêm món." : "Không thể thêm món vào đơn hàng.",
                        data = new { maDonHang = orderId }
                    });
                }
            }

            await restaurantHub.Clients.All.SendAsync("ReceiveDataUpdate", new
            {
                type = "order-updated",
                tableId = request.MaBan,
                orderId
            });

            return Json(new
            {
                success = true,
                message = created ? "Tạo đơn hàng thành công." : "Cập nhật đơn hàng thành công.",
                data = new { maDonHang = orderId, maChiTiet = detailId }
            });
        }
        catch (Exception exception)
        {
            return HandleError(exception, nameof(TaoHoacCapNhatOrder));
        }
    }

    [HttpPost("order")]
    public async Task<IActionResult> GuestOrder([FromBody] GuestOrderRequest? request)
    {
        try
        {
            if (request is null)
            {
                return Json(new { success = false, message = "Thiếu dữ liệu order.", data = (object?)null });
            }

            var orderId = orderBLL.AddGuestOrder(request.MaBan, request.Items);
            if (!orderId.HasValue)
            {
                return Json(new
                {
                    success = false,
                    message = "Không thể gửi order: bàn không hợp lệ, đã đặt/chờ thanh toán hoặc có món không bán.",
                    data = (object?)null
                });
            }

            await restaurantHub.Clients.All.SendAsync("ReceiveDataUpdate", new
            {
                type = "order-updated",
                tableId = request.MaBan,
                orderId = orderId.Value
            });

            return Json(new
            {
                success = true,
                message = "Order đã được gửi tới bếp.",
                data = new { maDonHang = orderId.Value }
            });
        }
        catch (Exception exception)
        {
            return HandleError(exception, nameof(GuestOrder));
        }
    }

    [HttpGet("menu")]
    public IActionResult GetDanhMucMonAn()
    {
        try
        {
            var data = orderBLL.GetMenuCategories();
            return Json(new { success = true, message = "Lấy danh mục món thành công.", data });
        }
        catch (Exception exception)
        {
            return HandleError(exception, nameof(GetDanhMucMonAn));
        }
    }

    [HttpGet("orders/table/{maBan:int}")]
    public IActionResult GetOrderPOS([FromRoute] int maBan)
    {
        try
        {
            var data = orderBLL.GetOpenOrderForTable(maBan);
            return Json(new
            {
                success = true,
                message = data is null ? "Bàn chưa có đơn đang mở." : "Lấy order thành công.",
                data
            });
        }
        catch (Exception exception)
        {
            return HandleError(exception, nameof(GetOrderPOS));
        }
    }

    [HttpGet("kitchen/orders")]
    public IActionResult GetDanhSachMonBep()
    {
        try
        {
            var data = orderBLL.GetKitchenItems();
            return Json(new { success = true, message = "Lấy danh sách món bếp thành công.", data });
        }
        catch (Exception exception)
        {
            return HandleError(exception, nameof(GetDanhSachMonBep));
        }
    }

    [HttpPut("kitchen/items/status")]
    public async Task<IActionResult> CapNhatTienDoBep([FromBody] CapNhatTienDoBepRequest? request)
    {
        try
        {
            if (request is null)
            {
                return Json(new { success = false, message = "Thiếu dữ liệu tiến độ bếp.", data = (object?)null });
            }

            var updated = orderBLL.UpdateKitchenProgress(request.MaChiTiet, request.TienDoBep);
            if (updated)
            {
                await restaurantHub.Clients.All.SendAsync("ReceiveDataUpdate", new
                {
                    type = "kitchen-progress",
                    detailId = request.MaChiTiet
                });
            }

            return Json(new
            {
                success = updated,
                message = updated ? "Cập nhật tiến độ bếp thành công." : "Không tìm thấy món thuộc đơn hàng đang mở.",
                data = updated
            });
        }
        catch (Exception exception)
        {
            return HandleError(exception, nameof(CapNhatTienDoBep));
        }
    }

    [HttpPost("payments")]
    public async Task<IActionResult> ThanhToanVaTruKho([FromBody] ThanhToanVaTruKhoRequest? request)
    {
        try
        {
            if (request is null)
            {
                return Json(new { success = false, message = "Thiếu dữ liệu thanh toán.", data = (object?)null });
            }

            var paid = hoaDonKhoBLL.ThanhToanVaTruKho(
                request.MaDonHang,
                request.MaThuNgan,
                request.HinhThucTra,
                request.TienGiamGia);

            if (paid)
            {
                await restaurantHub.Clients.All.SendAsync("ReceiveDataUpdate", new
                {
                    type = "payment-completed",
                    orderId = request.MaDonHang
                });
            }

            return Json(new
            {
                success = paid,
                message = paid ? "Thanh toán và trừ kho thành công." : "Không thể thanh toán: đơn không hợp lệ, chưa có món hoặc tồn kho không đủ.",
                data = paid
            });
        }
        catch (Exception exception)
        {
            return HandleError(exception, nameof(ThanhToanVaTruKho));
        }
    }

    [HttpGet("inventory/alerts")]
    public IActionResult GetCanhBaoTonKho()
    {
        try
        {
            var data = thongKeBLL.GetCanhBaoTonKho();
            return Json(new { success = true, message = "Lấy cảnh báo tồn kho thành công.", data });
        }
        catch (Exception exception)
        {
            return HandleError(exception, nameof(GetCanhBaoTonKho));
        }
    }

    [HttpGet("reports/revenue")]
    public IActionResult GetBaoCaoDoanhThu(
        [FromQuery] DateTime? tuNgay,
        [FromQuery] DateTime? denNgay,
        [FromQuery] bool theoGio = false)
    {
        try
        {
            var data = thongKeBLL.GetBaoCaoDoanhThu(tuNgay, denNgay, theoGio);
            return Json(new { success = true, message = "Lấy báo cáo doanh thu thành công.", data });
        }
        catch (Exception exception)
        {
            return HandleError(exception, nameof(GetBaoCaoDoanhThu));
        }
    }

    [HttpGet("reports/best-sellers")]
    public IActionResult GetMonBanChay([FromQuery] DateTime? tuNgay, [FromQuery] DateTime? denNgay)
    {
        try
        {
            var data = thongKeBLL.GetMonBanChay(tuNgay, denNgay);
            return Json(new { success = true, message = "Lấy báo cáo món bán chạy thành công.", data });
        }
        catch (Exception exception)
        {
            return HandleError(exception, nameof(GetMonBanChay));
        }
    }

    [HttpGet("reports/inventory-history")]
    public IActionResult GetLichSuTruKho([FromQuery] DateTime? tuNgay, [FromQuery] DateTime? denNgay)
    {
        try
        {
            var data = thongKeBLL.GetLichSuTruKho(tuNgay, denNgay);
            return Json(new { success = true, message = "Lấy lịch sử trừ kho thành công.", data });
        }
        catch (Exception exception)
        {
            return HandleError(exception, nameof(GetLichSuTruKho));
        }
    }

    private JsonResult HandleError(Exception exception, string actionName)
    {
        logger.LogError(exception, "Error in {ActionName}.", actionName);
        Response.StatusCode = exception is ArgumentException ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError;
        return Json(new
        {
            success = false,
            message = exception is ArgumentException ? exception.Message : "Đã xảy ra lỗi khi xử lý yêu cầu.",
            data = (object?)null
        });
    }
}
