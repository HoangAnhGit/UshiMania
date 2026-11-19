# Đặc tả Giao diện POS Cảm ứng Ushi Mania (Phong cách r_keeper)
- **Bố cục tổng thể:** Giao diện POS toàn màn hình cảm ứng (Touch-friendly), nút bấm dạng ô vuông lớn bo góc nhẹ, chữ rõ nét, không cuộn trang phức tạp.
- **Bảng màu đặc trưng:**
  - Nền chính: Xám nhạt (`#E9ECEF`), Header trên cùng: Xám đậm (`#5A6268`) chữ trắng.
  - Cột danh sách Order bên trái: Nền trắng, phần tóm tắt thông tin bàn/tiền phía trên nền xanh dương đậm (`#0033CC`) chữ trắng.
  - Lưới danh mục & món ăn ở giữa: Các nút ô vuông lớn nền xanh dương đậm/tím xanh (`#1A237E` / `#283593`) chữ trắng.
  - Bàn phím số (Numpad) bên phải: Nền xám đen (`#424242`) chữ trắng.
  - Các nút nghiệp vụ đáy màn hình: Nền xanh biển (`#1976D2`) cho *Chuyển Bàn, Gộp Bàn, Giảm Giá, Ghi Chú*; Nút *Xác nhận (✓)* màu xanh lá (`#2E7D32`); Nút *Hủy/Xóa (✕)* hoặc *Thanh Toán* màu đỏ (`#C62828`); Nút *Tạm tính* màu cam (`#EF6C00`).
- **Quy tắc bám sát đề tài:** Chỉ hiển thị đúng các chức năng có trong `RestaurantApiController.cs` (Bàn ăn, Gọi món + Ghi chú, Bếp KDS, Thanh toán VAT 8% + Tiền mặt/VietQR/Thẻ, Kho & Báo cáo). Bỏ qua các chức năng ngoài phạm vi như Capichi, Grab, Công nợ.