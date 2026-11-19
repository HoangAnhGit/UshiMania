# Quy tắc dự án Ushi Mania (BẮT BUỘC TUÂN THỦ)
- Tech stack: ASP.NET Web Forms (.NET Framework), ngôn ngữ C#, SQL Server (ADO.NET `SqlCommand`, `SqlTransaction`). Tuyệt đối KHÔNG dùng .NET Core, MVC, Razor hay Entity Framework.
- Kiến trúc 3 lớp: Presentation (`.aspx` / `.aspx.cs`) -> `BLL` -> `DAL`.
- Giao tiếp: Frontend dùng jQuery AJAX POST gọi tới `[WebMethod]` tĩnh trong `.aspx.cs`, trả về JSON `{ success, message, data }`.
- Quy tắc đặt tên: Bảng/cột SQL `snake_case`; Class C# `PascalCase`; biến/hàm `camelCase`; hằng số `UPPER_SNAKE_CASE`.
- Nghiệp vụ cố định:
  - Trạng thái bàn (`ban_an`): `TRONG`, `DANG_PHUC_VU`, `CHO_THANH_TOAN`, `DA_DAT`.
  - Tiến độ bếp (`chi_tiet_don_hang.tien_do_bep`): `'CHO_NAU'`, `'DANG_NAU'`, `'DA_XONG'`, có cột `ghi_chu_mon`.
  - Hóa đơn (`hoa_don`): `tien_thue_vat` tính cố định **8%** trên `tien_mon_an`. `hinh_thuc_tra` nhận `'TIEN_MAT'`, `'VIETQR'`, `'THE_NGAN_HANG'`.
  - Thanh toán & Trừ kho: Bắt buộc dùng `SqlTransaction` để lưu `hoa_don`, trừ tồn kho `nguyen_lieu` theo định lượng trong `recipes`, ghi lịch sử `inventory_logs` và đưa bàn về `'TRONG'`.