
INSERT INTO nguoi_dung (ten_dang_nhap, mat_khau_bam, ho_va_ten, vai_tro, so_dien_thoai)
VALUES
    ('quanly', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'QuanLy@123'), 2), N'Nguyễn Minh Anh', 'QUAN_LY', '0901000001'),
    ('phucvu', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'PhucVu@123'), 2), N'Trần Thảo Vy', 'PHUC_VU', '0901000002'),
    ('bep', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'Bep@123'), 2), N'Lê Quốc Bảo', 'BEP', '0901000003'),
    ('thungan', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'ThuNgan@123'), 2), N'Phạm Ngọc Linh', 'THU_NGAN', '0901000004');

INSERT INTO ban_an (so_hieu_ban, khu_vuc, suc_chua, trang_thai, ma_token_qr)
VALUES
    ('A01', N'Tầng 1 - Trong nhà', 2, N'TRONG', 'QR-USHI-A01'),
    ('A02', N'Tầng 1 - Trong nhà', 2, N'TRONG', 'QR-USHI-A02'),
    ('A03', N'Tầng 1 - Trong nhà', 4, N'TRONG', 'QR-USHI-A03'),
    ('A04', N'Tầng 1 - Trong nhà', 4, N'TRONG', 'QR-USHI-A04'),
    ('A05', N'Tầng 1 - Trong nhà', 6, N'TRONG', 'QR-USHI-A05'),
    ('A06', N'Tầng 1 - Trong nhà', 6, N'TRONG', 'QR-USHI-A06'),
    ('B01', N'Tầng 2 - Phòng riêng', 4, N'TRONG', 'QR-USHI-B01'),
    ('B02', N'Tầng 2 - Phòng riêng', 4, N'TRONG', 'QR-USHI-B02'),
    ('B03', N'Tầng 2 - Phòng riêng', 6, N'TRONG', 'QR-USHI-B03'),
    ('B04', N'Tầng 2 - Phòng riêng', 8, N'TRONG', 'QR-USHI-B04'),
    ('S01', N'Ban công', 2, N'TRONG', 'QR-USHI-S01'),
    ('S02', N'Ban công', 4, N'TRONG', 'QR-USHI-S02');

INSERT INTO danh_muc (ten_danh_muc, thu_tu_hien_thi, mo_ta)
VALUES
    (N'Bò nướng Nhật Bản', 1, N'Cắt thịt bò chất lượng cao, nướng tại bàn'),
    (N'Hải sản và món ăn kèm', 2, N'Hải sản tươi, cơm và món ăn kèm'),
    (N'Lẩu và món nóng', 3, N'Lẩu, mì và các món nóng kiểu Nhật - Hàn');

INSERT INTO mon_an (ma_danh_muc, ten_mon_an, don_gia, duong_dan_anh, noi_che_bien, trang_thai_ban, mo_ta_mon)
VALUES
    (1, N'Ba chỉ bò Wagyu nướng', 289000, 'images/wagyu-karubi.jpg', 'NUONG', 1, N'Ba chỉ bò Wagyu thái lát, nướng tại bàn'),
    (1, N'Lưỡi bò nướng muối', 219000, 'images/beef-tongue.jpg', 'NUONG', 1, N'Lưỡi bò thái mỏng, dùng cùng muối và chanh'),
    (1, N'Ba chỉ heo sốt yakiniku', 159000, 'images/pork-belly.jpg', 'NUONG', 1, N'Ba chỉ heo ướp sốt yakiniku'),
    (1, N'Đùi gà nướng tare', 149000, 'images/chicken-tare.jpg', 'NUONG', 1, N'Đùi gà lọc xương phủ sốt tare'),
    (1, N'Bò Wagyu hành tây', 269000, 'images/wagyu-onion.jpg', 'NUONG', 1, N'Wagyu ăn kèm hành tây và nước tương'),
    (2, N'Tôm sú nướng bơ tỏi', 189000, 'images/garlic-butter-shrimp.jpg', 'NUONG', 1, N'Tôm sú tươi nướng bơ và tỏi'),
    (2, N'Cá hồi nướng cơm Nhật', 179000, 'images/salmon-rice.jpg', 'NUONG', 1, N'Cá hồi nướng ăn kèm cơm Nhật'),
    (2, N'Cơm chiên kim chi', 79000, 'images/kimchi-fried-rice.jpg', 'CHIEN', 1, N'Cơm Nhật chiên cùng kim chi và hành lá'),
    (2, N'Đậu phụ hầm kim chi', 89000, 'images/kimchi-tofu.jpg', 'HAM', 1, N'Đậu phụ hầm cay cùng kim chi'),
    (2, N'Mì udon nấm', 99000, 'images/udon-mushroom.jpg', 'LUOC', 1, N'Mì udon nóng với nấm và hành lá'),
    (3, N'Nấm tổng hợp nướng bơ', 99000, 'images/butter-mushrooms.jpg', 'NUONG', 1, N'Nấm các loại nướng bơ thơm'),
    (3, N'Rau cuốn thịt heo kim chi', 129000, 'images/pork-kimchi-wrap.jpg', 'NUONG', 1, N'Thịt heo nướng cuốn xà lách và kim chi'),
    (3, N'Cơm tỏi bơ', 59000, 'images/garlic-butter-rice.jpg', 'CHIEN', 1, N'Cơm Nhật rang tỏi và bơ'),
    (3, N'Tôm nướng sốt cay', 179000, 'images/spicy-shrimp.jpg', 'NUONG', 1, N'Tôm nướng phủ sốt gochujang'),
    (3, N'Lẩu bò nấm đậu phụ', 329000, 'images/beef-mushroom-hotpot.jpg', 'LAU', 1, N'Nồi lẩu bò với nấm, đậu phụ và mì udon');

INSERT INTO nguyen_vat_lieu (ten_nguyen_lieu, don_vi_tinh, so_luong_ton, nguong_toi_thieu, han_su_dung)
VALUES
    (N'Thịt ba chỉ bò Wagyu', N'kg', 12.000, 3.000, '2027-06-30'),
    (N'Lưỡi bò', N'kg', 6.000, 1.500, '2027-06-30'),
    (N'Thịt ba chỉ heo', N'kg', 10.000, 2.000, '2027-06-30'),
    (N'Đùi gà lọc xương', N'kg', 8.000, 2.000, '2027-06-30'),
    (N'Tôm sú', N'kg', 7.000, 1.500, '2027-06-30'),
    (N'Cá hồi phi lê', N'kg', 5.000, 1.000, '2027-06-30'),
    (N'Gạo Nhật', N'kg', 25.000, 5.000, '2027-12-31'),
    (N'Kim chi cải thảo', N'kg', 8.000, 2.000, '2027-06-30'),
    (N'Xà lách', N'kg', 4.000, 1.000, '2027-06-30'),
    (N'Tỏi', N'kg', 3.000, 0.500, '2027-12-31'),
    (N'Hành lá', N'kg', 2.000, 0.500, '2027-06-30'),
    (N'Hành tây', N'kg', 5.000, 1.000, '2027-06-30'),
    (N'Nấm hương', N'kg', 3.000, 0.750, '2027-06-30'),
    (N'Nấm kim châm', N'kg', 3.000, 0.750, '2027-06-30'),
    (N'Đậu phụ', N'kg', 5.000, 1.000, '2027-06-30'),
    (N'Mì udon', N'kg', 6.000, 1.000, '2027-12-31'),
    (N'Nước tương Nhật', N'lít', 4.000, 0.500, '2027-12-31'),
    (N'Dầu mè', N'lít', 2.000, 0.300, '2027-12-31'),
    (N'Sốt gochujang', N'kg', 2.000, 0.300, '2027-12-31'),
    (N'Bơ lạt', N'kg', 3.000, 0.500, '2027-06-30');

INSERT INTO cong_thuc (ma_mon_an, ma_nguyen_lieu, dinh_luong)
VALUES
    (1, 1, 0.180), (1, 10, 0.010), (1, 17, 0.015),
    (2, 2, 0.160), (2, 10, 0.008), (2, 17, 0.010),
    (3, 3, 0.180), (3, 17, 0.020), (3, 18, 0.005),
    (4, 4, 0.180), (4, 17, 0.020), (4, 11, 0.008),
    (5, 1, 0.160), (5, 12, 0.040), (5, 17, 0.020),
    (6, 5, 0.160), (6, 20, 0.015), (6, 10, 0.010),
    (7, 6, 0.150), (7, 7, 0.120), (7, 17, 0.010),
    (8, 7, 0.180), (8, 8, 0.050), (8, 11, 0.010), (8, 20, 0.008),
    (9, 15, 0.150), (9, 8, 0.040), (9, 19, 0.015),
    (10, 16, 0.180), (10, 13, 0.030), (10, 11, 0.010), (10, 17, 0.015),
    (11, 13, 0.080), (11, 14, 0.050), (11, 20, 0.015),
    (12, 3, 0.120), (12, 8, 0.040), (12, 9, 0.050),
    (13, 7, 0.150), (13, 10, 0.010), (13, 20, 0.010),
    (14, 5, 0.160), (14, 19, 0.015), (14, 17, 0.010),
    (15, 1, 0.120), (15, 13, 0.050), (15, 14, 0.040), (15, 15, 0.080), (15, 16, 0.100), (15, 17, 0.020);
