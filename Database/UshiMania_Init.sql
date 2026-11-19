
CREATE TABLE nguoi_dung (
    ma_nguoi_dung INT IDENTITY(1,1) PRIMARY KEY,
    ten_dang_nhap VARCHAR(50) UNIQUE NOT NULL,
    mat_khau_bam VARCHAR(255) NOT NULL,
    ho_va_ten NVARCHAR(100) NOT NULL,
    vai_tro VARCHAR(20) NOT NULL,
    so_dien_thoai VARCHAR(15),
    trang_thai BIT DEFAULT 1,
    ngay_tao DATETIME DEFAULT GETDATE()
);

CREATE TABLE ban_an (
    ma_ban INT IDENTITY(1,1) PRIMARY KEY,
    so_hieu_ban VARCHAR(20) UNIQUE NOT NULL,
    khu_vuc NVARCHAR(50) NOT NULL,
    suc_chua INT NOT NULL CHECK (suc_chua > 0),
    trang_thai NVARCHAR(20) DEFAULT N'Trống',
    ma_token_qr VARCHAR(100)
);

CREATE TABLE dat_ban (
    ma_dat_ban INT IDENTITY(1,1) PRIMARY KEY,
    ma_ban INT,
    ten_khach_hang NVARCHAR(100) NOT NULL,
    so_dien_thoai VARCHAR(15) NOT NULL,
    so_luong_khach INT NOT NULL CHECK (so_luong_khach > 0),
    thoi_gian_den DATETIME NOT NULL,
    trang_thai NVARCHAR(20) DEFAULT N'Đã xác nhận',
    ghi_chu NVARCHAR(255),
    FOREIGN KEY (ma_ban) REFERENCES ban_an(ma_ban)
);

CREATE TABLE danh_muc (
    ma_danh_muc INT IDENTITY(1,1) PRIMARY KEY,
    ten_danh_muc NVARCHAR(100) NOT NULL,
    thu_tu_hien_thi INT DEFAULT 0,
    mo_ta NVARCHAR(255)
);

CREATE TABLE mon_an (
    ma_mon_an INT IDENTITY(1,1) PRIMARY KEY,
    ma_danh_muc INT,
    ten_mon_an NVARCHAR(100) NOT NULL,
    don_gia DECIMAL(18,2) NOT NULL CHECK (don_gia >= 0),
    duong_dan_anh VARCHAR(255),
    noi_che_bien VARCHAR(20) NOT NULL,
    trang_thai_ban BIT DEFAULT 1,
    mo_ta_mon NVARCHAR(500),
    FOREIGN KEY (ma_danh_muc) REFERENCES danh_muc(ma_danh_muc)
);

CREATE TABLE don_hang (
    ma_don_hang INT IDENTITY(1,1) PRIMARY KEY,
    ma_ban INT,
    ma_nhan_vien INT,
    trang_thai_don VARCHAR(20) DEFAULT 'DANG_MO',
    thoi_gian_tao DATETIME DEFAULT GETDATE(),
    tong_tien_tam DECIMAL(18,2) DEFAULT 0,
    FOREIGN KEY (ma_ban) REFERENCES ban_an(ma_ban),
    FOREIGN KEY (ma_nhan_vien) REFERENCES nguoi_dung(ma_nguoi_dung)
);

CREATE TABLE chi_tiet_don_hang (
    ma_chi_tiet INT IDENTITY(1,1) PRIMARY KEY,
    ma_don_hang INT,
    ma_mon_an INT,
    so_luong INT NOT NULL CHECK (so_luong > 0),
    don_gia_luc_goi DECIMAL(18,2) NOT NULL,
    ghi_chu_mon NVARCHAR(200),
    tien_do_bep VARCHAR(20) DEFAULT 'CHO_NAU',
    thoi_gian_goi DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (ma_don_hang) REFERENCES don_hang(ma_don_hang),
    FOREIGN KEY (ma_mon_an) REFERENCES mon_an(ma_mon_an)
);

CREATE TABLE hoa_don (
    ma_hoa_don INT IDENTITY(1,1) PRIMARY KEY,
    ma_don_hang INT UNIQUE,
    ma_thu_ngan INT,
    tien_mon_an DECIMAL(18,2) NOT NULL,
    tien_thue_vat DECIMAL(18,2) NOT NULL,
    tien_giam_gia DECIMAL(18,2) DEFAULT 0,
    tong_thanh_toan DECIMAL(18,2) NOT NULL,
    hinh_thuc_tra VARCHAR(20) NOT NULL,
    ngay_thanh_toan DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (ma_don_hang) REFERENCES don_hang(ma_don_hang),
    FOREIGN KEY (ma_thu_ngan) REFERENCES nguoi_dung(ma_nguoi_dung)
);

CREATE TABLE nguyen_vat_lieu (
    ma_nguyen_lieu INT IDENTITY(1,1) PRIMARY KEY,
    ten_nguyen_lieu NVARCHAR(100) NOT NULL,
    don_vi_tinh NVARCHAR(20) NOT NULL,
    so_luong_ton DECIMAL(18,3) DEFAULT 0,
    nguong_toi_thieu DECIMAL(18,3) NOT NULL,
    han_su_dung DATE
);

CREATE TABLE cong_thuc (
    ma_cong_thuc INT IDENTITY(1,1) PRIMARY KEY,
    ma_mon_an INT,
    ma_nguyen_lieu INT,
    dinh_luong DECIMAL(18,3) NOT NULL CHECK (dinh_luong > 0),
    FOREIGN KEY (ma_mon_an) REFERENCES mon_an(ma_mon_an),
    FOREIGN KEY (ma_nguyen_lieu) REFERENCES nguyen_vat_lieu(ma_nguyen_lieu)
);

CREATE TABLE phieu_nhap_kho (
    ma_phieu_nhap INT IDENTITY(1,1) PRIMARY KEY,
    ma_nguyen_lieu INT,
    so_luong_nhap DECIMAL(18,3) NOT NULL CHECK (so_luong_nhap > 0),
    don_gia_nhap DECIMAL(18,2) NOT NULL CHECK (don_gia_nhap >= 0),
    nha_cung_cap NVARCHAR(200) NOT NULL,
    ma_nguoi_nhap INT,
    ngay_nhap DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (ma_nguyen_lieu) REFERENCES nguyen_vat_lieu(ma_nguyen_lieu),
    FOREIGN KEY (ma_nguoi_nhap) REFERENCES nguoi_dung(ma_nguoi_dung)
);

CREATE TABLE nhat_ky_kho (
    ma_nhat_ky INT IDENTITY(1,1) PRIMARY KEY,
    ma_nguyen_lieu INT,
    ma_don_hang INT NULL,
    loai_bien_dong VARCHAR(20) NOT NULL,
    so_luong_thay_doi DECIMAL(18,3) NOT NULL,
    thoi_gian DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (ma_nguyen_lieu) REFERENCES nguyen_vat_lieu(ma_nguyen_lieu),
    FOREIGN KEY (ma_don_hang) REFERENCES don_hang(ma_don_hang)
);
