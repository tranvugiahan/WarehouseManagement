/* =====================================================================
   HỆ THỐNG QUẢN LÝ KHO BÃI - warehouse_db_sqlserver.sql
   DBMS    : SQL Server 2016 SP1 trở lên (dùng THROW, CREATE OR ALTER,
             AT TIME ZONE, ISJSON, FOR JSON)
   Nguồn   : SRS (mục 5, 6) + ERD
   Cách chạy: mở trong SSMS / Azure Data Studio rồi Execute toàn bộ,
              hoặc: sqlcmd -S <server> -E -I -f 65001 -i warehouse_db_sqlserver.sql
              (-I bật QUOTED_IDENTIFIER, -f 65001 đọc file UTF-8)
   Cấu trúc : 1) Database + hàm giờ  2) Bảng  3) Trigger
              4) Procedure  5) View  6) Dữ liệu mẫu (tùy chọn)
   Lưu ý    : file bắt đầu bằng việc XÓA database warehouse_db nếu đã tồn tại.
   ===================================================================== */

/* #####################################################################
   1. DATABASE
   ##################################################################### */
USE master;
GO
IF DB_ID(N'warehouse_db') IS NOT NULL
BEGIN
    ALTER DATABASE warehouse_db SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE warehouse_db;
END
GO
CREATE DATABASE warehouse_db COLLATE Vietnamese_CI_AS;
GO
USE warehouse_db;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- Giờ hiện tại theo GMT+7 (NFR-08), độc lập múi giờ của máy chủ
CREATE FUNCTION dbo.fn_now()
RETURNS DATETIME2(0)
AS
BEGIN
    RETURN CAST(SYSDATETIMEOFFSET() AT TIME ZONE N'SE Asia Standard Time' AS DATETIME2(0));
END
GO

/* #####################################################################
   2. BẢNG
   Trạng thái phiếu: DRAFT=Nháp, PENDING=Chờ duyệt, APPROVED=Đã duyệt,
                     REJECTED=Bị từ chối, CANCELLED=Đã hủy
   ##################################################################### */

-- 2.1 USERS (FR1, FR2, BR-10)
CREATE TABLE dbo.users (
    id             INT IDENTITY(1,1) NOT NULL,
    full_name      NVARCHAR(100) NOT NULL,
    username       NVARCHAR(50)  NOT NULL,
    email          NVARCHAR(150) NOT NULL,
    password_hash  VARCHAR(255)  NOT NULL,                        -- NFR-03: chỉ lưu hash
    role           VARCHAR(10)   NOT NULL CONSTRAINT df_users_role   DEFAULT 'EMPLOYEE',
    status         VARCHAR(10)   NOT NULL CONSTRAINT df_users_status DEFAULT 'ACTIVE',
    last_login_at  DATETIME2(0)  NULL,
    created_at     DATETIME2(0)  NOT NULL CONSTRAINT df_users_created DEFAULT (dbo.fn_now()),
    updated_at     DATETIME2(0)  NOT NULL CONSTRAINT df_users_updated DEFAULT (dbo.fn_now()),
    CONSTRAINT pk_users PRIMARY KEY (id),
    CONSTRAINT uq_users_username UNIQUE (username),
    CONSTRAINT uq_users_email    UNIQUE (email),
    CONSTRAINT chk_users_role    CHECK (role   IN ('ADMIN','MANAGER','EMPLOYEE')),
    CONSTRAINT chk_users_status  CHECK (status IN ('ACTIVE','INACTIVE'))
);
GO

-- 2.2 DANH MỤC (FR3)
CREATE TABLE dbo.categories (
    id      INT IDENTITY(1,1) NOT NULL,
    name    NVARCHAR(100) NOT NULL,
    status  VARCHAR(10)   NOT NULL CONSTRAINT df_categories_status DEFAULT 'ACTIVE',
    CONSTRAINT pk_categories PRIMARY KEY (id),
    CONSTRAINT uq_categories_name UNIQUE (name),
    CONSTRAINT chk_categories_status CHECK (status IN ('ACTIVE','INACTIVE'))
);

CREATE TABLE dbo.units (
    id      INT IDENTITY(1,1) NOT NULL,
    name    NVARCHAR(50) NOT NULL,
    status  VARCHAR(10)  NOT NULL CONSTRAINT df_units_status DEFAULT 'ACTIVE',
    CONSTRAINT pk_units PRIMARY KEY (id),
    CONSTRAINT uq_units_name UNIQUE (name),
    CONSTRAINT chk_units_status CHECK (status IN ('ACTIVE','INACTIVE'))
);

CREATE TABLE dbo.suppliers (
    id       INT IDENTITY(1,1) NOT NULL,
    name     NVARCHAR(150) NOT NULL,
    phone    VARCHAR(20)   NULL,
    email    VARCHAR(150)  NULL,
    address  NVARCHAR(255) NULL,
    status   VARCHAR(10)   NOT NULL CONSTRAINT df_suppliers_status DEFAULT 'ACTIVE',
    CONSTRAINT pk_suppliers PRIMARY KEY (id),
    CONSTRAINT chk_suppliers_status CHECK (status IN ('ACTIVE','INACTIVE'))
);
CREATE INDEX idx_suppliers_name ON dbo.suppliers (name);
GO

-- 2.3 PRODUCTS (FR3.2, FR3.7, BR-01, BR-09, BR-14) - không lưu NSX/HSD cụ thể
CREATE TABLE dbo.products (
    id             INT IDENTITY(1,1) NOT NULL,
    sku            VARCHAR(50)   NOT NULL,
    barcode        VARCHAR(50)   NULL,
    name           NVARCHAR(200) NOT NULL,
    category_id    INT           NOT NULL,
    unit_id        INT           NOT NULL,
    cost_price     DECIMAL(15,2) NOT NULL CONSTRAINT df_products_cost    DEFAULT 0,
    export_price   DECIMAL(15,2) NOT NULL CONSTRAINT df_products_export DEFAULT 0,
    minimum_stock  INT           NOT NULL CONSTRAINT df_products_min    DEFAULT 0,
    has_expiry     BIT           NOT NULL CONSTRAINT df_products_expiry DEFAULT 0,
    status         VARCHAR(10)   NOT NULL CONSTRAINT df_products_status DEFAULT 'ACTIVE',  -- INACTIVE = ngừng kinh doanh
    created_at     DATETIME2(0)  NOT NULL CONSTRAINT df_products_created DEFAULT (dbo.fn_now()),
    CONSTRAINT pk_products PRIMARY KEY (id),
    CONSTRAINT uq_products_sku UNIQUE (sku),
    CONSTRAINT fk_products_category FOREIGN KEY (category_id) REFERENCES dbo.categories (id),
    CONSTRAINT fk_products_unit     FOREIGN KEY (unit_id)     REFERENCES dbo.units (id),
    CONSTRAINT chk_products_status        CHECK (status IN ('ACTIVE','INACTIVE')),
    CONSTRAINT chk_products_cost_price    CHECK (cost_price >= 0),
    CONSTRAINT chk_products_export_price  CHECK (export_price >= 0),
    CONSTRAINT chk_products_minimum_stock CHECK (minimum_stock >= 0),
    CONSTRAINT chk_products_sku_blank     CHECK (LEN(LTRIM(RTRIM(sku))) > 0),
    CONSTRAINT chk_products_name_blank    CHECK (LEN(LTRIM(RTRIM(name))) > 0)
);
-- barcode duy nhất, nhưng cho phép nhiều sản phẩm không có barcode (NULL)
CREATE UNIQUE INDEX uq_products_barcode ON dbo.products (barcode) WHERE barcode IS NOT NULL;
CREATE INDEX idx_products_name     ON dbo.products (name);
CREATE INDEX idx_products_category ON dbo.products (category_id);
CREATE INDEX idx_products_unit     ON dbo.products (unit_id);
GO

-- 2.4 INVENTORY (thực thể yếu của Product; chỉ lưu TỔNG tồn - BR-18)
CREATE TABLE dbo.inventory (
    product_id  INT NOT NULL,
    quantity    INT NOT NULL CONSTRAINT df_inventory_qty DEFAULT 0,
    updated_at  DATETIME2(0) NOT NULL CONSTRAINT df_inventory_updated DEFAULT (dbo.fn_now()),
    CONSTRAINT pk_inventory PRIMARY KEY (product_id),
    CONSTRAINT fk_inventory_product FOREIGN KEY (product_id) REFERENCES dbo.products (id),
    CONSTRAINT chk_inventory_quantity CHECK (quantity >= 0)       -- không cho tồn âm (NFR-09)
);
GO

-- 2.5 PHIẾU NHẬP (FR6, BR-02, BR-07, BR-14)
CREATE TABLE dbo.import_orders (
    id               INT IDENTITY(1,1) NOT NULL,
    code             VARCHAR(30)   NOT NULL,
    supplier_id      INT           NOT NULL,
    created_by       INT           NOT NULL,
    approved_by      INT           NULL,
    import_date      DATE          NOT NULL,
    status           VARCHAR(10)   NOT NULL CONSTRAINT df_io_status DEFAULT 'DRAFT',
    rejection_reason NVARCHAR(500) NULL,
    created_at       DATETIME2(0)  NOT NULL CONSTRAINT df_io_created DEFAULT (dbo.fn_now()),
    CONSTRAINT pk_import_orders PRIMARY KEY (id),
    CONSTRAINT uq_import_orders_code UNIQUE (code),
    CONSTRAINT fk_io_supplier    FOREIGN KEY (supplier_id) REFERENCES dbo.suppliers (id),
    CONSTRAINT fk_io_created_by  FOREIGN KEY (created_by)  REFERENCES dbo.users (id),
    CONSTRAINT fk_io_approved_by FOREIGN KEY (approved_by) REFERENCES dbo.users (id),
    CONSTRAINT chk_io_status CHECK (status IN ('DRAFT','PENDING','APPROVED','REJECTED','CANCELLED')),
    -- BR-07: bị từ chối phải có lý do
    CONSTRAINT chk_io_rejection CHECK (status <> 'REJECTED'
        OR (rejection_reason IS NOT NULL AND LEN(LTRIM(RTRIM(rejection_reason))) > 0)),
    CONSTRAINT chk_io_approver CHECK (status NOT IN ('APPROVED','CANCELLED') OR approved_by IS NOT NULL)
);
CREATE INDEX idx_io_supplier    ON dbo.import_orders (supplier_id);
CREATE INDEX idx_io_created_by  ON dbo.import_orders (created_by);
CREATE INDEX idx_io_approved_by ON dbo.import_orders (approved_by);
CREATE INDEX idx_io_status_date ON dbo.import_orders (status, import_date);
GO

CREATE TABLE dbo.import_order_details (
    id                  INT IDENTITY(1,1) NOT NULL,
    import_order_id     INT           NOT NULL,
    product_id          INT           NOT NULL,
    quantity            INT           NOT NULL,
    unit_price          DECIMAL(15,2) NOT NULL,
    manufacturing_date  DATE          NULL,     -- NSX theo từng dòng nhập (không có Batch/Lot)
    expiry_date         DATE          NULL,     -- HSD; bắt buộc nếu product.has_expiry = 1 (trigger)
    CONSTRAINT pk_import_order_details PRIMARY KEY (id),
    CONSTRAINT fk_iod_order   FOREIGN KEY (import_order_id) REFERENCES dbo.import_orders (id) ON DELETE CASCADE,
    CONSTRAINT fk_iod_product FOREIGN KEY (product_id)      REFERENCES dbo.products (id),
    CONSTRAINT chk_iod_quantity   CHECK (quantity > 0),                       -- BR-02
    CONSTRAINT chk_iod_unit_price CHECK (unit_price >= 0),
    CONSTRAINT chk_iod_dates      CHECK (expiry_date IS NULL OR manufacturing_date IS NULL
                                         OR expiry_date > manufacturing_date) -- BR-14
);
CREATE INDEX idx_iod_order   ON dbo.import_order_details (import_order_id);
CREATE INDEX idx_iod_product ON dbo.import_order_details (product_id);
CREATE INDEX idx_iod_expiry  ON dbo.import_order_details (expiry_date);
GO

-- 2.6 PHIẾU XUẤT (FR7, BR-02, BR-03, BR-15, BR-16)
--     type: DELIVERY = xuất giao hàng, DAMAGE = hư hỏng, EXPIRED = hết hạn
CREATE TABLE dbo.export_orders (
    id               INT IDENTITY(1,1) NOT NULL,
    code             VARCHAR(30)   NOT NULL,
    type             VARCHAR(10)   NOT NULL,
    destination      NVARCHAR(255) NULL,       -- nơi nhận hàng, nhập trực tiếp (không có bảng khách hàng)
    export_date      DATE          NOT NULL,
    created_by       INT           NOT NULL,
    approved_by      INT           NULL,
    reason           NVARCHAR(500) NULL,       -- lý do xuất (bắt buộc với DAMAGE/EXPIRED)
    rejection_reason NVARCHAR(500) NULL,
    status           VARCHAR(10)   NOT NULL CONSTRAINT df_eo_status DEFAULT 'DRAFT',
    created_at       DATETIME2(0)  NOT NULL CONSTRAINT df_eo_created DEFAULT (dbo.fn_now()),
    CONSTRAINT pk_export_orders PRIMARY KEY (id),
    CONSTRAINT uq_export_orders_code UNIQUE (code),
    CONSTRAINT fk_eo_created_by  FOREIGN KEY (created_by)  REFERENCES dbo.users (id),
    CONSTRAINT fk_eo_approved_by FOREIGN KEY (approved_by) REFERENCES dbo.users (id),
    CONSTRAINT chk_eo_type   CHECK (type   IN ('DELIVERY','DAMAGE','EXPIRED')),
    CONSTRAINT chk_eo_status CHECK (status IN ('DRAFT','PENDING','APPROVED','REJECTED','CANCELLED')),
    -- DELIVERY: bắt buộc destination; DAMAGE/EXPIRED: bắt buộc reason và không dùng destination
    CONSTRAINT chk_eo_type_fields CHECK (
           (type = 'DELIVERY'
              AND destination IS NOT NULL AND LEN(LTRIM(RTRIM(destination))) > 0)
        OR (type IN ('DAMAGE','EXPIRED')
              AND destination IS NULL
              AND reason IS NOT NULL AND LEN(LTRIM(RTRIM(reason))) > 0)),
    CONSTRAINT chk_eo_rejection CHECK (status <> 'REJECTED'
        OR (rejection_reason IS NOT NULL AND LEN(LTRIM(RTRIM(rejection_reason))) > 0)),
    CONSTRAINT chk_eo_approver CHECK (status NOT IN ('APPROVED','CANCELLED') OR approved_by IS NOT NULL)
);
CREATE INDEX idx_eo_created_by       ON dbo.export_orders (created_by);
CREATE INDEX idx_eo_approved_by      ON dbo.export_orders (approved_by);
CREATE INDEX idx_eo_type_status_date ON dbo.export_orders (type, status, export_date);
CREATE INDEX idx_eo_destination      ON dbo.export_orders (destination);
GO

CREATE TABLE dbo.export_order_details (
    id               INT IDENTITY(1,1) NOT NULL,
    export_order_id  INT           NOT NULL,
    product_id       INT           NOT NULL,
    quantity         INT           NOT NULL,
    unit_price       DECIMAL(15,2) NOT NULL,   -- mặc định = Product.export_price (xem sp_add_export_item)
    CONSTRAINT pk_export_order_details PRIMARY KEY (id),
    CONSTRAINT fk_eod_order   FOREIGN KEY (export_order_id) REFERENCES dbo.export_orders (id) ON DELETE CASCADE,
    CONSTRAINT fk_eod_product FOREIGN KEY (product_id)      REFERENCES dbo.products (id),
    CONSTRAINT chk_eod_quantity   CHECK (quantity > 0),                       -- BR-02
    CONSTRAINT chk_eod_unit_price CHECK (unit_price >= 0)
);
CREATE INDEX idx_eod_order   ON dbo.export_order_details (export_order_id);
CREATE INDEX idx_eod_product ON dbo.export_order_details (product_id);
GO

-- 2.7 KIỂM KÊ (FR10, BR-12) - StockTakeDetail là thực thể yếu: PK (stock_take_id, product_id)
CREATE TABLE dbo.stock_takes (
    id               INT IDENTITY(1,1) NOT NULL,
    code             VARCHAR(30)   NOT NULL,
    created_by       INT           NOT NULL,
    approved_by      INT           NULL,
    status           VARCHAR(10)   NOT NULL CONSTRAINT df_st_status DEFAULT 'DRAFT',
    rejection_reason NVARCHAR(500) NULL,
    created_at       DATETIME2(0)  NOT NULL CONSTRAINT df_st_created DEFAULT (dbo.fn_now()),
    CONSTRAINT pk_stock_takes PRIMARY KEY (id),
    CONSTRAINT uq_stock_takes_code UNIQUE (code),
    CONSTRAINT fk_st_created_by  FOREIGN KEY (created_by)  REFERENCES dbo.users (id),
    CONSTRAINT fk_st_approved_by FOREIGN KEY (approved_by) REFERENCES dbo.users (id),
    CONSTRAINT chk_st_status CHECK (status IN ('DRAFT','PENDING','APPROVED','REJECTED','CANCELLED')),
    CONSTRAINT chk_st_rejection CHECK (status <> 'REJECTED'
        OR (rejection_reason IS NOT NULL AND LEN(LTRIM(RTRIM(rejection_reason))) > 0)),
    CONSTRAINT chk_st_approver CHECK (status <> 'APPROVED' OR approved_by IS NOT NULL)
);
CREATE INDEX idx_st_created_by  ON dbo.stock_takes (created_by);
CREATE INDEX idx_st_approved_by ON dbo.stock_takes (approved_by);
CREATE INDEX idx_st_status      ON dbo.stock_takes (status);
GO

CREATE TABLE dbo.stock_take_details (
    stock_take_id   INT NOT NULL,
    product_id      INT NOT NULL,
    system_quantity INT NOT NULL,                  -- tồn hệ thống tại thời điểm tạo phiếu
    actual_quantity INT NULL,                      -- số đếm thực tế (nhập sau)
    difference      AS (actual_quantity - system_quantity) PERSISTED,   -- FR10.4
    CONSTRAINT pk_stock_take_details PRIMARY KEY (stock_take_id, product_id),
    CONSTRAINT fk_std_stock_take FOREIGN KEY (stock_take_id) REFERENCES dbo.stock_takes (id) ON DELETE CASCADE,
    CONSTRAINT fk_std_product    FOREIGN KEY (product_id)     REFERENCES dbo.products (id),
    CONSTRAINT chk_std_system_qty CHECK (system_quantity >= 0),
    CONSTRAINT chk_std_actual_qty CHECK (actual_quantity IS NULL OR actual_quantity >= 0)
);
CREATE INDEX idx_std_product ON dbo.stock_take_details (product_id);
GO

-- 2.8 STOCK MOVEMENT (BR-13, BR-17) - append-only
--     quantity mang DẤU: dương = tăng tồn, âm = giảm tồn.
--     reference_type: BỔ SUNG so với SRS để reference_id trỏ đúng bảng (cần cho REVERSAL).
CREATE TABLE dbo.stock_movements (
    id               BIGINT IDENTITY(1,1) NOT NULL,
    product_id       INT          NOT NULL,
    type             VARCHAR(20)  NOT NULL,
    reference_type   VARCHAR(20)  NOT NULL,
    reference_id     INT          NOT NULL,
    quantity         INT          NOT NULL,
    quantity_before  INT          NOT NULL,
    quantity_after   INT          NOT NULL,
    created_by       INT          NOT NULL,
    created_at       DATETIME2(0) NOT NULL CONSTRAINT df_sm_created DEFAULT (dbo.fn_now()),
    CONSTRAINT pk_stock_movements PRIMARY KEY (id),
    CONSTRAINT fk_sm_product    FOREIGN KEY (product_id) REFERENCES dbo.products (id),
    CONSTRAINT fk_sm_created_by FOREIGN KEY (created_by) REFERENCES dbo.users (id),
    CONSTRAINT chk_sm_type CHECK (type IN ('IMPORT','EXPORT_DELIVERY','EXPORT_DAMAGE','EXPORT_EXPIRED',
                                           'STOCKTAKE_ADJUST','REVERSAL')),
    CONSTRAINT chk_sm_reftype CHECK (reference_type IN ('IMPORT_ORDER','EXPORT_ORDER','STOCK_TAKE')),
    CONSTRAINT chk_sm_nonzero CHECK (quantity <> 0),
    CONSTRAINT chk_sm_balance CHECK (quantity_after = quantity_before + quantity),
    CONSTRAINT chk_sm_non_negative CHECK (quantity_before >= 0 AND quantity_after >= 0),
    CONSTRAINT chk_sm_direction CHECK (
           (type = 'IMPORT' AND quantity > 0)
        OR (type IN ('EXPORT_DELIVERY','EXPORT_DAMAGE','EXPORT_EXPIRED') AND quantity < 0)
        OR type IN ('STOCKTAKE_ADJUST','REVERSAL'))
);
CREATE INDEX idx_sm_product_time ON dbo.stock_movements (product_id, created_at);
CREATE INDEX idx_sm_reference    ON dbo.stock_movements (reference_type, reference_id);
CREATE INDEX idx_sm_created_by   ON dbo.stock_movements (created_by);
GO

-- 2.9 STATUS HISTORY (FR5.7, BR-17) - append-only; document_id trỏ 1 trong 3 bảng phiếu (không đặt FK)
CREATE TABLE dbo.status_history (
    id             BIGINT IDENTITY(1,1) NOT NULL,
    document_type  VARCHAR(20)  NOT NULL,
    document_id    INT          NOT NULL,
    old_status     VARCHAR(10)  NULL,              -- NULL khi tạo mới
    new_status     VARCHAR(10)  NOT NULL,
    reason         NVARCHAR(500) NULL,
    changed_by     INT          NOT NULL,
    changed_at     DATETIME2(0) NOT NULL CONSTRAINT df_sh_changed DEFAULT (dbo.fn_now()),
    CONSTRAINT pk_status_history PRIMARY KEY (id),
    CONSTRAINT fk_sh_changed_by FOREIGN KEY (changed_by) REFERENCES dbo.users (id),
    CONSTRAINT chk_sh_doctype CHECK (document_type IN ('IMPORT_ORDER','EXPORT_ORDER','STOCK_TAKE')),
    CONSTRAINT chk_sh_old CHECK (old_status IS NULL OR old_status IN ('DRAFT','PENDING','APPROVED','REJECTED','CANCELLED')),
    CONSTRAINT chk_sh_new CHECK (new_status IN ('DRAFT','PENDING','APPROVED','REJECTED','CANCELLED'))
);
CREATE INDEX idx_sh_document   ON dbo.status_history (document_type, document_id);
CREATE INDEX idx_sh_changed_by ON dbo.status_history (changed_by);
GO

-- 2.10 AUDIT LOG (NFR-05, BR-17) - append-only
CREATE TABLE dbo.audit_logs (
    id            BIGINT IDENTITY(1,1) NOT NULL,
    user_id       INT           NOT NULL,
    action        VARCHAR(50)   NOT NULL,    -- LOGIN, CREATE, SUBMIT, APPROVE, REJECT, CANCEL, ADJUST...
    module        VARCHAR(50)   NOT NULL,    -- AUTH, IMPORT, EXPORT, STOCKTAKE, PRODUCT...
    reference_id  BIGINT        NULL,
    metadata      NVARCHAR(MAX) NULL,        -- JSON
    created_at    DATETIME2(0)  NOT NULL CONSTRAINT df_al_created DEFAULT (dbo.fn_now()),
    CONSTRAINT pk_audit_logs PRIMARY KEY (id),
    CONSTRAINT fk_al_user FOREIGN KEY (user_id) REFERENCES dbo.users (id),
    CONSTRAINT chk_al_metadata_json CHECK (metadata IS NULL OR ISJSON(metadata) = 1)
);
CREATE INDEX idx_al_user_time   ON dbo.audit_logs (user_id, created_at);
CREATE INDEX idx_al_module_time ON dbo.audit_logs (module, created_at);
GO

/* #####################################################################
   3. TRIGGER
   Mẫu xử lý lỗi: ROLLBACK giao dịch hiện tại rồi THROW, nên phía gọi
   (procedure / ứng dụng) nhận được lỗi 5xxxx kèm thông báo tiếng Việt.
   ##################################################################### */

-- 3.1 Tự tạo dòng tồn kho = 0 khi thêm sản phẩm
CREATE OR ALTER TRIGGER dbo.trg_products_ai ON dbo.products
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.inventory (product_id, quantity)
    SELECT id, 0 FROM inserted;
END
GO

-- 3.2 Tự cập nhật users.updated_at
CREATE OR ALTER TRIGGER dbo.trg_users_au ON dbo.users
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL(OBJECT_ID(N'dbo.trg_users_au')) > 1 RETURN;
    UPDATE u SET updated_at = dbo.fn_now()
      FROM dbo.users u JOIN inserted i ON i.id = u.id;
END
GO

-- 3.3 Chi tiết phiếu nhập: BR-04 (chỉ Nháp), BR-09 (SP ngừng KD), BR-14 (has_expiry => expiry_date)
CREATE OR ALTER TRIGGER dbo.trg_iod_iud ON dbo.import_order_details
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM inserted) AND NOT EXISTS (SELECT 1 FROM deleted) RETURN;

    IF EXISTS (SELECT 1
                 FROM (SELECT import_order_id FROM inserted
                       UNION
                       SELECT import_order_id FROM deleted) x
                 JOIN dbo.import_orders o ON o.id = x.import_order_id
                WHERE o.status <> 'DRAFT')
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50010, N'BR-04: Chỉ phiếu ở trạng thái Nháp mới được sửa/xóa chi tiết', 1;
    END

    IF EXISTS (SELECT 1
                 FROM inserted i
                 JOIN dbo.products p ON p.id = i.product_id
                 LEFT JOIN deleted d ON d.id = i.id
                WHERE p.status <> 'ACTIVE'
                  AND (d.id IS NULL OR d.product_id <> i.product_id))
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50011, N'BR-09: Sản phẩm ngừng kinh doanh không được chọn cho giao dịch mới', 1;
    END

    IF EXISTS (SELECT 1
                 FROM inserted i
                 JOIN dbo.products p ON p.id = i.product_id
                WHERE p.has_expiry = 1 AND i.expiry_date IS NULL)
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50012, N'BR-14: Sản phẩm có quản lý HSD bắt buộc nhập expiry_date', 1;
    END
END
GO

-- 3.4 Chi tiết phiếu xuất: BR-04, BR-09
CREATE OR ALTER TRIGGER dbo.trg_eod_iud ON dbo.export_order_details
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM inserted) AND NOT EXISTS (SELECT 1 FROM deleted) RETURN;

    IF EXISTS (SELECT 1
                 FROM (SELECT export_order_id FROM inserted
                       UNION
                       SELECT export_order_id FROM deleted) x
                 JOIN dbo.export_orders o ON o.id = x.export_order_id
                WHERE o.status <> 'DRAFT')
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50013, N'BR-04: Chỉ phiếu ở trạng thái Nháp mới được sửa/xóa chi tiết', 1;
    END

    IF EXISTS (SELECT 1
                 FROM inserted i
                 JOIN dbo.products p ON p.id = i.product_id
                 LEFT JOIN deleted d ON d.id = i.id
                WHERE p.status <> 'ACTIVE'
                  AND (d.id IS NULL OR d.product_id <> i.product_id))
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50014, N'BR-09: Sản phẩm ngừng kinh doanh không được chọn cho giao dịch mới', 1;
    END
END
GO

-- 3.5 Chi tiết kiểm kê: chỉ sửa khi phiếu Nháp
CREATE OR ALTER TRIGGER dbo.trg_std_iud ON dbo.stock_take_details
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM inserted) AND NOT EXISTS (SELECT 1 FROM deleted) RETURN;

    IF EXISTS (SELECT 1
                 FROM (SELECT stock_take_id FROM inserted
                       UNION
                       SELECT stock_take_id FROM deleted) x
                 JOIN dbo.stock_takes s ON s.id = x.stock_take_id
                WHERE s.status <> 'DRAFT')
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50015, N'BR-04: Chỉ phiếu ở trạng thái Nháp mới được sửa/xóa chi tiết', 1;
    END
END
GO

-- 3.6 Phiếu xuất: chỉ Admin/Manager được lập phiếu DAMAGE / EXPIRED
CREATE OR ALTER TRIGGER dbo.trg_eo_ai ON dbo.export_orders
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1
                 FROM inserted i
                 LEFT JOIN dbo.users u ON u.id = i.created_by
                WHERE i.type IN ('DAMAGE','EXPIRED')
                  AND ISNULL(u.role, '') NOT IN ('ADMIN','MANAGER'))
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50016, N'Chỉ Admin/Manager được lập phiếu hư hỏng/hết hạn', 1;
    END
END
GO

-- 3.7 Máy trạng thái phiếu (FR5.1):
--     DRAFT -> PENDING -> APPROVED | REJECTED ; REJECTED -> DRAFT ; APPROVED -> CANCELLED
--     (phiếu kiểm kê không có bước hủy)
CREATE OR ALTER TRIGGER dbo.trg_io_au ON dbo.import_orders
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(status) RETURN;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.id = i.id
                WHERE i.status <> d.status
                  AND NOT (   (d.status = 'DRAFT'    AND i.status = 'PENDING')
                           OR (d.status = 'PENDING'  AND i.status IN ('APPROVED','REJECTED'))
                           OR (d.status = 'REJECTED' AND i.status = 'DRAFT')
                           OR (d.status = 'APPROVED' AND i.status = 'CANCELLED')))
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50017, N'FR5.1: Chuyển trạng thái phiếu nhập không hợp lệ', 1;
    END
END
GO

CREATE OR ALTER TRIGGER dbo.trg_eo_au ON dbo.export_orders
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(status) RETURN;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.id = i.id
                WHERE i.status <> d.status
                  AND NOT (   (d.status = 'DRAFT'    AND i.status = 'PENDING')
                           OR (d.status = 'PENDING'  AND i.status IN ('APPROVED','REJECTED'))
                           OR (d.status = 'REJECTED' AND i.status = 'DRAFT')
                           OR (d.status = 'APPROVED' AND i.status = 'CANCELLED')))
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50018, N'FR5.1: Chuyển trạng thái phiếu xuất không hợp lệ', 1;
    END
END
GO

CREATE OR ALTER TRIGGER dbo.trg_st_au ON dbo.stock_takes
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(status) RETURN;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.id = i.id
                WHERE i.status <> d.status
                  AND NOT (   (d.status = 'DRAFT'    AND i.status = 'PENDING')
                           OR (d.status = 'PENDING'  AND i.status IN ('APPROVED','REJECTED'))
                           OR (d.status = 'REJECTED' AND i.status = 'DRAFT')))
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50019, N'FR5.1: Chuyển trạng thái phiếu kiểm kê không hợp lệ', 1;
    END
END
GO

-- 3.8 BR-17: chỉ phiếu Nháp mới được xóa vật lý
CREATE OR ALTER TRIGGER dbo.trg_io_ad ON dbo.import_orders
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted WHERE status <> 'DRAFT')
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50020, N'BR-17: Chỉ phiếu Nháp mới được xóa', 1;
    END
END
GO

CREATE OR ALTER TRIGGER dbo.trg_eo_ad ON dbo.export_orders
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted WHERE status <> 'DRAFT')
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50021, N'BR-17: Chỉ phiếu Nháp mới được xóa', 1;
    END
END
GO

CREATE OR ALTER TRIGGER dbo.trg_st_ad ON dbo.stock_takes
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted WHERE status <> 'DRAFT')
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50022, N'BR-17: Chỉ phiếu Nháp mới được xóa', 1;
    END
END
GO

-- 3.9 BR-17: StockMovement / StatusHistory / AuditLog là append-only
CREATE OR ALTER TRIGGER dbo.trg_sm_block ON dbo.stock_movements
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    THROW 50023, N'BR-17: Không được sửa/xóa StockMovement', 1;
END
GO

CREATE OR ALTER TRIGGER dbo.trg_sh_block ON dbo.status_history
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    THROW 50024, N'BR-17: Không được sửa/xóa StatusHistory', 1;
END
GO

CREATE OR ALTER TRIGGER dbo.trg_al_block ON dbo.audit_logs
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    THROW 50025, N'BR-17: Không được sửa/xóa AuditLog', 1;
END
GO

/* #####################################################################
   4. PROCEDURE
   Mọi thao tác đổi tồn kho chạy trong 1 transaction, khóa dòng inventory
   bằng UPDLOCK + HOLDLOCK trước khi kiểm tra/cập nhật (NFR-09). Nếu 2 phiên
   tranh nhau cùng lúc, SQL Server có thể chọn 1 phiên làm nạn nhân deadlock
   (lỗi 1205): ứng dụng nên bắt lỗi này và thử lại.
   ##################################################################### */

-- Kiểu bảng truyền danh sách thay đổi tồn: delta dương = tăng, âm = giảm
IF TYPE_ID(N'dbo.StockDeltaTable') IS NULL
BEGIN
    CREATE TYPE dbo.StockDeltaTable AS TABLE
    (
        product_id INT NOT NULL PRIMARY KEY,
        delta      INT NOT NULL
    );
END
GO

-- BR-11: chỉ Admin/Manager đang hoạt động
CREATE OR ALTER PROCEDURE dbo.sp_assert_approver @user_id INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @role VARCHAR(10), @status VARCHAR(10);
    SELECT @role = role, @status = status FROM dbo.users WHERE id = @user_id;
    IF @role IS NULL OR @status <> 'ACTIVE' OR @role NOT IN ('ADMIN','MANAGER')
    BEGIN
        THROW 50001, N'BR-11: Chỉ Admin/Manager đang hoạt động mới có quyền thực hiện', 1;
    END
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_log_status
    @doc VARCHAR(20), @id INT, @old VARCHAR(10), @new VARCHAR(10),
    @reason NVARCHAR(500), @user_id INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.status_history (document_type, document_id, old_status, new_status, reason, changed_by)
    VALUES (@doc, @id, @old, @new, @reason, @user_id);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_audit
    @user_id INT, @action VARCHAR(50), @module VARCHAR(50),
    @ref BIGINT = NULL, @meta NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.audit_logs (user_id, action, module, reference_id, metadata)
    VALUES (@user_id, @action, @module, @ref, @meta);
END
GO

-- Áp dụng danh sách thay đổi tồn: khóa -> kiểm tra không âm -> cập nhật -> ghi StockMovement.
-- Phải gọi bên trong transaction của procedure nghiệp vụ.
CREATE OR ALTER PROCEDURE dbo.sp_apply_stock_delta
    @lines    dbo.StockDeltaTable READONLY,
    @type     VARCHAR(20),
    @ref_type VARCHAR(20),
    @ref_id   INT,
    @user_id  INT,
    @context  NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    IF @@TRANCOUNT = 0
    BEGIN
        THROW 50031, N'sp_apply_stock_delta phải được gọi trong một transaction', 1;
    END

    DECLARE @locked INT, @bad INT, @before INT, @delta INT, @msg NVARCHAR(400);

    -- khóa toàn bộ dòng tồn liên quan
    SELECT @locked = COUNT(*)
      FROM dbo.inventory i WITH (UPDLOCK, HOLDLOCK)
      JOIN @lines l ON l.product_id = i.product_id;

    IF @locked <> (SELECT COUNT(*) FROM @lines)
    BEGIN
        THROW 50033, N'Có sản phẩm chưa có dòng tồn kho', 1;
    END

    -- kiểm tra tồn sau thay đổi không âm (BR-03/BR-15/BR-08)
    SELECT TOP (1) @bad = l.product_id, @before = i.quantity, @delta = l.delta
      FROM @lines l
      JOIN dbo.inventory i ON i.product_id = l.product_id
     WHERE i.quantity + l.delta < 0
     ORDER BY l.product_id;

    IF @bad IS NOT NULL
    BEGIN
        SET @msg = CONCAT(@context, N', sản phẩm id=', @bad,
                          N' (tồn ', @before, N', thay đổi ', @delta, N')');
        THROW 50032, @msg, 1;
    END

    DECLARE @mv TABLE (product_id INT, before_qty INT, after_qty INT);

    UPDATE i
       SET quantity   = i.quantity + l.delta,
           updated_at = dbo.fn_now()
    OUTPUT inserted.product_id, deleted.quantity, inserted.quantity
      INTO @mv (product_id, before_qty, after_qty)
      FROM dbo.inventory i
      JOIN @lines l ON l.product_id = i.product_id;

    INSERT INTO dbo.stock_movements
        (product_id, type, reference_type, reference_id, quantity, quantity_before, quantity_after, created_by)
    SELECT product_id, @type, @ref_type, @ref_id, after_qty - before_qty, before_qty, after_qty, @user_id
      FROM @mv
     WHERE after_qty <> before_qty;
END
GO

-- BR-03 / FR7.2: kiểm tra tồn lần 1 (gộp theo sản phẩm)
CREATE OR ALTER PROCEDURE dbo.sp_check_export_stock @export_order_id INT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1
                 FROM (SELECT product_id, SUM(quantity) AS qty
                         FROM dbo.export_order_details
                        WHERE export_order_id = @export_order_id
                        GROUP BY product_id) x
                 JOIN dbo.inventory i ON i.product_id = x.product_id
                WHERE x.qty > i.quantity)
    BEGIN
        THROW 50034, N'BR-03: Số lượng xuất vượt quá tồn kho khả dụng', 1;
    END
END
GO

-- Thêm dòng vào phiếu xuất (BR-16, BR-03)
--    @unit_price = NULL -> lấy Product.export_price; khác mặc định -> chỉ Admin/Manager
CREATE OR ALTER PROCEDURE dbo.sp_add_export_item
    @order_id   INT,
    @product_id INT,
    @quantity   INT,
    @unit_price DECIMAL(15,2) = NULL,
    @user_id    INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @role VARCHAR(10), @default DECIMAL(15,2), @price DECIMAL(15,2),
            @stock INT = 0, @in_order INT = 0;

    SELECT @role = role FROM dbo.users WHERE id = @user_id;
    SELECT @default = export_price FROM dbo.products WHERE id = @product_id;

    IF @default IS NULL
    BEGIN
        THROW 50035, N'Không tìm thấy sản phẩm', 1;
    END

    IF @unit_price IS NULL
        SET @price = @default;
    ELSE IF @unit_price <> @default AND ISNULL(@role, '') NOT IN ('ADMIN','MANAGER')
    BEGIN
        THROW 50036, N'BR-16: Chỉ Admin/Manager được điều chỉnh đơn giá xuất', 1;
    END
    ELSE
        SET @price = @unit_price;

    SELECT @stock = quantity FROM dbo.inventory WHERE product_id = @product_id;
    SELECT @in_order = ISNULL(SUM(quantity), 0)
      FROM dbo.export_order_details
     WHERE export_order_id = @order_id AND product_id = @product_id;

    IF @in_order + @quantity > @stock
    BEGIN
        THROW 50034, N'BR-03: Số lượng xuất vượt quá tồn kho khả dụng', 1;
    END

    -- trigger trg_eod_iud kiểm tra phiếu còn Nháp và sản phẩm còn kinh doanh
    INSERT INTO dbo.export_order_details (export_order_id, product_id, quantity, unit_price)
    VALUES (@order_id, @product_id, @quantity, @price);
END
GO

-- GỬI DUYỆT (FR5.2): DRAFT -> PENDING
CREATE OR ALTER PROCEDURE dbo.sp_submit_import_order @id INT, @user_id INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @status VARCHAR(10), @cnt INT;
    BEGIN TRY
        BEGIN TRAN;
        SELECT @status = status FROM dbo.import_orders WITH (UPDLOCK, HOLDLOCK) WHERE id = @id;
        IF @status IS NULL  BEGIN THROW 50040, N'Không tìm thấy phiếu nhập', 1; END
        IF @status <> 'DRAFT' BEGIN THROW 50041, N'Chỉ phiếu Nháp mới được gửi duyệt', 1; END
        SELECT @cnt = COUNT(*) FROM dbo.import_order_details WHERE import_order_id = @id;
        IF @cnt = 0 BEGIN THROW 50042, N'Phiếu chưa có chi tiết, không thể gửi duyệt', 1; END

        UPDATE dbo.import_orders SET status = 'PENDING' WHERE id = @id;
        EXEC dbo.sp_log_status 'IMPORT_ORDER', @id, 'DRAFT', 'PENDING', NULL, @user_id;
        EXEC dbo.sp_audit @user_id, 'SUBMIT', 'IMPORT', @id;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_submit_export_order @id INT, @user_id INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @status VARCHAR(10), @cnt INT;
    BEGIN TRY
        BEGIN TRAN;
        SELECT @status = status FROM dbo.export_orders WITH (UPDLOCK, HOLDLOCK) WHERE id = @id;
        IF @status IS NULL  BEGIN THROW 50043, N'Không tìm thấy phiếu xuất', 1; END
        IF @status <> 'DRAFT' BEGIN THROW 50041, N'Chỉ phiếu Nháp mới được gửi duyệt', 1; END
        SELECT @cnt = COUNT(*) FROM dbo.export_order_details WHERE export_order_id = @id;
        IF @cnt = 0 BEGIN THROW 50042, N'Phiếu chưa có chi tiết, không thể gửi duyệt', 1; END

        EXEC dbo.sp_check_export_stock @id;                     -- kiểm tra tồn lần 1 (BR-03)

        UPDATE dbo.export_orders SET status = 'PENDING' WHERE id = @id;
        EXEC dbo.sp_log_status 'EXPORT_ORDER', @id, 'DRAFT', 'PENDING', NULL, @user_id;
        EXEC dbo.sp_audit @user_id, 'SUBMIT', 'EXPORT', @id;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_submit_stock_take @id INT, @user_id INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @status VARCHAR(10), @cnt INT, @missing INT;
    BEGIN TRY
        BEGIN TRAN;
        SELECT @status = status FROM dbo.stock_takes WITH (UPDLOCK, HOLDLOCK) WHERE id = @id;
        IF @status IS NULL  BEGIN THROW 50044, N'Không tìm thấy phiếu kiểm kê', 1; END
        IF @status <> 'DRAFT' BEGIN THROW 50041, N'Chỉ phiếu Nháp mới được gửi duyệt', 1; END

        SELECT @cnt = COUNT(*),
               @missing = ISNULL(SUM(CASE WHEN actual_quantity IS NULL THEN 1 ELSE 0 END), 0)
          FROM dbo.stock_take_details WHERE stock_take_id = @id;
        IF @cnt = 0 BEGIN THROW 50045, N'Phiếu kiểm kê chưa có sản phẩm', 1; END
        IF @missing > 0 BEGIN THROW 50046, N'Còn sản phẩm chưa nhập số lượng thực tế', 1; END

        UPDATE dbo.stock_takes SET status = 'PENDING' WHERE id = @id;
        EXEC dbo.sp_log_status 'STOCK_TAKE', @id, 'DRAFT', 'PENDING', NULL, @user_id;
        EXEC dbo.sp_audit @user_id, 'SUBMIT', 'STOCKTAKE', @id;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO

-- DUYỆT PHIẾU NHẬP (FR6.3, FR6.4, BR-05, BR-13): cộng tồn + StockMovement
CREATE OR ALTER PROCEDURE dbo.sp_approve_import_order @id INT, @user_id INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @status VARCHAR(10);
    DECLARE @lines dbo.StockDeltaTable;
    BEGIN TRY
        EXEC dbo.sp_assert_approver @user_id;
        BEGIN TRAN;
        SELECT @status = status FROM dbo.import_orders WITH (UPDLOCK, HOLDLOCK) WHERE id = @id;
        IF @status IS NULL    BEGIN THROW 50040, N'Không tìm thấy phiếu nhập', 1; END
        IF @status <> 'PENDING' BEGIN THROW 50047, N'Chỉ phiếu Chờ duyệt mới được duyệt', 1; END

        INSERT INTO @lines (product_id, delta)
        SELECT product_id, SUM(quantity)
          FROM dbo.import_order_details WHERE import_order_id = @id
         GROUP BY product_id;

        EXEC dbo.sp_apply_stock_delta @lines = @lines, @type = 'IMPORT', @ref_type = 'IMPORT_ORDER',
             @ref_id = @id, @user_id = @user_id, @context = N'Lỗi cập nhật tồn';

        UPDATE dbo.import_orders SET status = 'APPROVED', approved_by = @user_id WHERE id = @id;
        EXEC dbo.sp_log_status 'IMPORT_ORDER', @id, 'PENDING', 'APPROVED', NULL, @user_id;
        EXEC dbo.sp_audit @user_id, 'APPROVE', 'IMPORT', @id;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO

-- DUYỆT PHIẾU XUẤT (FR7.3, FR7.4, FR7.6, BR-06, BR-15)
--    Kiểm tra tồn lần 2 trong transaction, sau khi đã khóa dòng tồn.
--    Thiếu tồn -> lỗi -> ROLLBACK, phiếu giữ nguyên Chờ duyệt.
CREATE OR ALTER PROCEDURE dbo.sp_approve_export_order @id INT, @user_id INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @status VARCHAR(10), @type VARCHAR(10), @mtype VARCHAR(20), @meta NVARCHAR(MAX);
    DECLARE @lines dbo.StockDeltaTable;
    BEGIN TRY
        EXEC dbo.sp_assert_approver @user_id;
        BEGIN TRAN;
        SELECT @status = status, @type = type
          FROM dbo.export_orders WITH (UPDLOCK, HOLDLOCK) WHERE id = @id;
        IF @status IS NULL    BEGIN THROW 50043, N'Không tìm thấy phiếu xuất', 1; END
        IF @status <> 'PENDING' BEGIN THROW 50047, N'Chỉ phiếu Chờ duyệt mới được duyệt', 1; END

        SET @mtype = CASE @type WHEN 'DELIVERY' THEN 'EXPORT_DELIVERY'
                                WHEN 'DAMAGE'   THEN 'EXPORT_DAMAGE'
                                ELSE 'EXPORT_EXPIRED' END;

        INSERT INTO @lines (product_id, delta)
        SELECT product_id, -SUM(quantity)
          FROM dbo.export_order_details WHERE export_order_id = @id
         GROUP BY product_id;

        EXEC dbo.sp_apply_stock_delta @lines = @lines, @type = @mtype, @ref_type = 'EXPORT_ORDER',
             @ref_id = @id, @user_id = @user_id, @context = N'BR-15: Không đủ tồn kho';

        UPDATE dbo.export_orders SET status = 'APPROVED', approved_by = @user_id WHERE id = @id;
        EXEC dbo.sp_log_status 'EXPORT_ORDER', @id, 'PENDING', 'APPROVED', NULL, @user_id;
        SET @meta = (SELECT @type AS [type] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        EXEC dbo.sp_audit @user_id, 'APPROVE', 'EXPORT', @id, @meta;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO

-- DUYỆT KIỂM KÊ (FR10.5, FR10.6, BR-12)
--    Cộng phần CHÊNH LỆCH vào tồn hiện tại (không ghi đè bằng số thực tế).
CREATE OR ALTER PROCEDURE dbo.sp_approve_stock_take @id INT, @user_id INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @status VARCHAR(10);
    DECLARE @lines dbo.StockDeltaTable;
    BEGIN TRY
        EXEC dbo.sp_assert_approver @user_id;
        BEGIN TRAN;
        SELECT @status = status FROM dbo.stock_takes WITH (UPDLOCK, HOLDLOCK) WHERE id = @id;
        IF @status IS NULL    BEGIN THROW 50044, N'Không tìm thấy phiếu kiểm kê', 1; END
        IF @status <> 'PENDING' BEGIN THROW 50047, N'Chỉ phiếu Chờ duyệt mới được duyệt', 1; END

        INSERT INTO @lines (product_id, delta)
        SELECT product_id, difference
          FROM dbo.stock_take_details
         WHERE stock_take_id = @id AND difference <> 0;

        EXEC dbo.sp_apply_stock_delta @lines = @lines, @type = 'STOCKTAKE_ADJUST', @ref_type = 'STOCK_TAKE',
             @ref_id = @id, @user_id = @user_id, @context = N'Điều chỉnh làm tồn âm';

        UPDATE dbo.stock_takes SET status = 'APPROVED', approved_by = @user_id WHERE id = @id;
        EXEC dbo.sp_log_status 'STOCK_TAKE', @id, 'PENDING', 'APPROVED', NULL, @user_id;
        EXEC dbo.sp_audit @user_id, 'ADJUST', 'STOCKTAKE', @id;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO

-- TỪ CHỐI (FR5.4, BR-07): PENDING -> REJECTED, bắt buộc có lý do
--    @doc: 'IMPORT_ORDER' | 'EXPORT_ORDER' | 'STOCK_TAKE'
CREATE OR ALTER PROCEDURE dbo.sp_reject_order
    @doc VARCHAR(20), @id INT, @user_id INT, @reason NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @status VARCHAR(10), @meta NVARCHAR(MAX);
    BEGIN TRY
        EXEC dbo.sp_assert_approver @user_id;
        IF @reason IS NULL OR LEN(LTRIM(RTRIM(@reason))) = 0
        BEGIN THROW 50048, N'BR-07: Từ chối phiếu phải kèm lý do', 1; END

        BEGIN TRAN;
        IF @doc = 'IMPORT_ORDER'
            SELECT @status = status FROM dbo.import_orders WITH (UPDLOCK, HOLDLOCK) WHERE id = @id;
        ELSE IF @doc = 'EXPORT_ORDER'
            SELECT @status = status FROM dbo.export_orders WITH (UPDLOCK, HOLDLOCK) WHERE id = @id;
        ELSE IF @doc = 'STOCK_TAKE'
            SELECT @status = status FROM dbo.stock_takes   WITH (UPDLOCK, HOLDLOCK) WHERE id = @id;
        ELSE
        BEGIN THROW 50049, N'Loại phiếu không hợp lệ', 1; END

        IF @status IS NULL      BEGIN THROW 50050, N'Không tìm thấy phiếu', 1; END
        IF @status <> 'PENDING' BEGIN THROW 50051, N'Chỉ phiếu Chờ duyệt mới được từ chối', 1; END

        IF @doc = 'IMPORT_ORDER'
            UPDATE dbo.import_orders SET status = 'REJECTED', rejection_reason = @reason WHERE id = @id;
        ELSE IF @doc = 'EXPORT_ORDER'
            UPDATE dbo.export_orders SET status = 'REJECTED', rejection_reason = @reason WHERE id = @id;
        ELSE
            UPDATE dbo.stock_takes   SET status = 'REJECTED', rejection_reason = @reason WHERE id = @id;

        EXEC dbo.sp_log_status @doc, @id, 'PENDING', 'REJECTED', @reason, @user_id;
        SET @meta = (SELECT @reason AS reason FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        EXEC dbo.sp_audit @user_id, 'REJECT', @doc, @id, @meta;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO

-- TRẢ VỀ NHÁP (FR5.5): REJECTED -> DRAFT để chỉnh sửa và gửi lại
CREATE OR ALTER PROCEDURE dbo.sp_reopen_order @doc VARCHAR(20), @id INT, @user_id INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @status VARCHAR(10);
    BEGIN TRY
        BEGIN TRAN;
        IF @doc = 'IMPORT_ORDER'
            SELECT @status = status FROM dbo.import_orders WITH (UPDLOCK, HOLDLOCK) WHERE id = @id;
        ELSE IF @doc = 'EXPORT_ORDER'
            SELECT @status = status FROM dbo.export_orders WITH (UPDLOCK, HOLDLOCK) WHERE id = @id;
        ELSE IF @doc = 'STOCK_TAKE'
            SELECT @status = status FROM dbo.stock_takes   WITH (UPDLOCK, HOLDLOCK) WHERE id = @id;
        ELSE
        BEGIN THROW 50049, N'Loại phiếu không hợp lệ', 1; END

        IF @status IS NULL        BEGIN THROW 50050, N'Không tìm thấy phiếu', 1; END
        IF @status <> 'REJECTED'  BEGIN THROW 50052, N'Chỉ phiếu Bị từ chối mới được trả về Nháp', 1; END

        IF @doc = 'IMPORT_ORDER'
            UPDATE dbo.import_orders SET status = 'DRAFT' WHERE id = @id;
        ELSE IF @doc = 'EXPORT_ORDER'
            UPDATE dbo.export_orders SET status = 'DRAFT' WHERE id = @id;
        ELSE
            UPDATE dbo.stock_takes   SET status = 'DRAFT' WHERE id = @id;

        EXEC dbo.sp_log_status @doc, @id, 'REJECTED', 'DRAFT', NULL, @user_id;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO

-- HỦY PHIẾU ĐÃ DUYỆT (FR5.6, BR-08): hoàn tác tồn bằng StockMovement REVERSAL
--    @doc: 'IMPORT_ORDER' (trừ lại tồn) | 'EXPORT_ORDER' (cộng lại tồn)
--    Không cho hủy nếu tồn sau hoàn tác < 0.
CREATE OR ALTER PROCEDURE dbo.sp_cancel_order
    @doc VARCHAR(20), @id INT, @user_id INT, @reason NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @status VARCHAR(10), @meta NVARCHAR(MAX);
    DECLARE @lines dbo.StockDeltaTable;
    BEGIN TRY
        EXEC dbo.sp_assert_approver @user_id;
        IF @reason IS NULL OR LEN(LTRIM(RTRIM(@reason))) = 0
        BEGIN THROW 50053, N'BR-08: Hủy phiếu phải ghi lý do', 1; END
        IF @doc NOT IN ('IMPORT_ORDER','EXPORT_ORDER')
        BEGIN THROW 50054, N'Chỉ hủy được phiếu nhập hoặc phiếu xuất', 1; END

        BEGIN TRAN;
        IF @doc = 'IMPORT_ORDER'
            SELECT @status = status FROM dbo.import_orders WITH (UPDLOCK, HOLDLOCK) WHERE id = @id;
        ELSE
            SELECT @status = status FROM dbo.export_orders WITH (UPDLOCK, HOLDLOCK) WHERE id = @id;

        IF @status IS NULL       BEGIN THROW 50050, N'Không tìm thấy phiếu', 1; END
        IF @status <> 'APPROVED' BEGIN THROW 50055, N'Chỉ phiếu Đã duyệt mới được hủy', 1; END

        IF @doc = 'IMPORT_ORDER'        -- hủy phiếu nhập => giảm tồn
            INSERT INTO @lines (product_id, delta)
            SELECT product_id, -SUM(quantity)
              FROM dbo.import_order_details WHERE import_order_id = @id
             GROUP BY product_id;
        ELSE                            -- hủy phiếu xuất => tăng tồn
            INSERT INTO @lines (product_id, delta)
            SELECT product_id, SUM(quantity)
              FROM dbo.export_order_details WHERE export_order_id = @id
             GROUP BY product_id;

        EXEC dbo.sp_apply_stock_delta @lines = @lines, @type = 'REVERSAL', @ref_type = @doc,
             @ref_id = @id, @user_id = @user_id, @context = N'BR-08: Không thể hủy vì tồn sẽ âm';

        IF @doc = 'IMPORT_ORDER'
            UPDATE dbo.import_orders SET status = 'CANCELLED' WHERE id = @id;
        ELSE
            UPDATE dbo.export_orders SET status = 'CANCELLED' WHERE id = @id;

        EXEC dbo.sp_log_status @doc, @id, 'APPROVED', 'CANCELLED', @reason, @user_id;
        SET @meta = (SELECT @reason AS reason FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        EXEC dbo.sp_audit @user_id, 'CANCEL', @doc, @id, @meta;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO

-- TẠO PHIẾU KIỂM KÊ (FR10.1, FR10.2)
--    @category_id = NULL -> toàn bộ sản phẩm đang kinh doanh; chụp tồn hệ thống vào system_quantity
CREATE OR ALTER PROCEDURE dbo.sp_create_stock_take
    @code          VARCHAR(30),
    @user_id       INT,
    @category_id   INT = NULL,
    @stock_take_id INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @id INT, @cnt INT;
    BEGIN TRY
        BEGIN TRAN;
        INSERT INTO dbo.stock_takes (code, created_by) VALUES (@code, @user_id);
        SET @id = CAST(SCOPE_IDENTITY() AS INT);

        INSERT INTO dbo.stock_take_details (stock_take_id, product_id, system_quantity)
        SELECT @id, p.id, i.quantity
          FROM dbo.products p
          JOIN dbo.inventory i ON i.product_id = p.id
         WHERE p.status = 'ACTIVE'
           AND (@category_id IS NULL OR p.category_id = @category_id);
        SET @cnt = @@ROWCOUNT;

        IF @cnt = 0 BEGIN THROW 50056, N'Không có sản phẩm nào để kiểm kê', 1; END

        EXEC dbo.sp_log_status 'STOCK_TAKE', @id, NULL, 'DRAFT', NULL, @user_id;
        EXEC dbo.sp_audit @user_id, 'CREATE', 'STOCKTAKE', @id;
        COMMIT;
        SET @stock_take_id = @id;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO

/* #####################################################################
   5. VIEW
   ##################################################################### */

-- FR9.1, FR9.2, FR11.2: tồn kho theo sản phẩm (kèm nhóm hàng, giá trị tồn)
CREATE OR ALTER VIEW dbo.v_inventory_status AS
SELECT p.id            AS product_id,
       p.sku,
       p.barcode,
       p.name          AS product_name,
       c.id            AS category_id,
       c.name          AS category_name,
       u.name          AS unit_name,
       p.status        AS product_status,
       i.quantity,
       p.minimum_stock,
       CAST(CASE WHEN i.quantity <= p.minimum_stock THEN 1 ELSE 0 END AS BIT) AS is_low_stock,
       p.cost_price,
       CAST(i.quantity AS DECIMAL(18,2)) * p.cost_price AS stock_value,
       i.updated_at
  FROM dbo.products p
  JOIN dbo.inventory  i ON i.product_id = p.id
  JOIN dbo.categories c ON c.id = p.category_id
  JOIN dbo.units      u ON u.id = p.unit_id;
GO

-- FR9.3: cảnh báo tồn thấp
CREATE OR ALTER VIEW dbo.v_low_stock_alerts AS
SELECT *
  FROM dbo.v_inventory_status
 WHERE product_status = 'ACTIVE'
   AND quantity <= minimum_stock;
GO

-- FR9.4, FR9.5: cảnh báo HSD dựa trên dòng nhập ĐÃ DUYỆT của sản phẩm has_expiry còn tồn > 0.
--    days_to_expiry < 0 -> đã hết hạn. Ngưỡng "sắp hết hạn" lọc ở tầng ứng dụng.
--    Chỉ mang tính tham khảo vì không theo dõi tồn riêng theo từng dòng nhập (BR-18).
CREATE OR ALTER VIEW dbo.v_expiry_alerts AS
SELECT p.id           AS product_id,
       p.sku,
       p.name         AS product_name,
       io.id          AS import_order_id,
       io.code        AS import_code,
       io.import_date,
       d.id           AS import_detail_id,
       d.quantity     AS imported_quantity,
       d.manufacturing_date,
       d.expiry_date,
       DATEDIFF(DAY, CAST(dbo.fn_now() AS DATE), d.expiry_date) AS days_to_expiry,
       i.quantity     AS current_stock
  FROM dbo.import_order_details d
  JOIN dbo.import_orders io ON io.id = d.import_order_id AND io.status = 'APPROVED'
  JOIN dbo.products      p  ON p.id  = d.product_id      AND p.has_expiry = 1
  JOIN dbo.inventory      i  ON i.product_id = p.id
 WHERE d.expiry_date IS NOT NULL
   AND i.quantity > 0;
GO

-- FR4.5: tổng số lượng / giá trị phiếu nhập
CREATE OR ALTER VIEW dbo.v_import_order_totals AS
SELECT io.id, io.code, io.import_date, io.status, io.supplier_id,
       s.name AS supplier_name,
       COUNT(d.id)                                   AS line_count,
       ISNULL(SUM(d.quantity), 0)                    AS total_quantity,
       ISNULL(SUM(d.quantity * d.unit_price), 0)     AS total_amount
  FROM dbo.import_orders io
  JOIN dbo.suppliers s ON s.id = io.supplier_id
  LEFT JOIN dbo.import_order_details d ON d.import_order_id = io.id
 GROUP BY io.id, io.code, io.import_date, io.status, io.supplier_id, s.name;
GO

-- FR4.5: tổng số lượng / giá trị phiếu xuất
CREATE OR ALTER VIEW dbo.v_export_order_totals AS
SELECT eo.id, eo.code, eo.type, eo.destination, eo.export_date, eo.status,
       COUNT(d.id)                                   AS line_count,
       ISNULL(SUM(d.quantity), 0)                    AS total_quantity,
       ISNULL(SUM(d.quantity * d.unit_price), 0)     AS total_amount
  FROM dbo.export_orders eo
  LEFT JOIN dbo.export_order_details d ON d.export_order_id = eo.id
 GROUP BY eo.id, eo.code, eo.type, eo.destination, eo.export_date, eo.status;
GO

-- FR11.4: xuất giao hàng theo destination / sản phẩm / tháng (chỉ phiếu Đã duyệt)
CREATE OR ALTER VIEW dbo.v_export_by_destination AS
SELECT eo.destination,
       CONVERT(CHAR(7), eo.export_date, 120) AS export_month,
       eo.export_date,
       p.id   AS product_id,
       p.sku,
       p.name AS product_name,
       SUM(d.quantity)                 AS total_quantity,
       SUM(d.quantity * d.unit_price) AS total_amount
  FROM dbo.export_orders eo
  JOIN dbo.export_order_details d ON d.export_order_id = eo.id
  JOIN dbo.products p ON p.id = d.product_id
 WHERE eo.type = 'DELIVERY' AND eo.status = 'APPROVED'
 GROUP BY eo.destination, CONVERT(CHAR(7), eo.export_date, 120), eo.export_date,
          p.id, p.sku, p.name;
GO

-- FR11.5: xuất hư hỏng / hết hạn (chỉ phiếu Đã duyệt)
CREATE OR ALTER VIEW dbo.v_damage_expired_report AS
SELECT eo.id AS export_order_id, eo.code, eo.type, eo.export_date, eo.reason,
       p.id AS product_id, p.sku, p.name AS product_name,
       d.quantity,
       d.quantity * d.unit_price AS amount
  FROM dbo.export_orders eo
  JOIN dbo.export_order_details d ON d.export_order_id = eo.id
  JOIN dbo.products p ON p.id = d.product_id
 WHERE eo.type IN ('DAMAGE','EXPIRED') AND eo.status = 'APPROVED';
GO

-- FR9.6: lịch sử biến động tồn
CREATE OR ALTER VIEW dbo.v_stock_movement_history AS
SELECT sm.id, sm.created_at, sm.product_id, p.sku, p.name AS product_name,
       sm.type, sm.reference_type, sm.reference_id,
       sm.quantity, sm.quantity_before, sm.quantity_after,
       sm.created_by, u.full_name AS created_by_name
  FROM dbo.stock_movements sm
  JOIN dbo.products p ON p.id = sm.product_id
  JOIN dbo.users    u ON u.id = sm.created_by;
GO

/* #####################################################################
   6. DỮ LIỆU MẪU (CHỈ DÙNG MÔI TRƯỜNG DEV - xóa phần này nếu không cần)
   Database mới nên IDENTITY bắt đầu từ 1: users 1=Admin, 2=Manager, 3=Employee.
   !! password_hash chỉ là chuỗi giữ chỗ, hãy thay bằng bcrypt/argon2 do backend sinh.
   ##################################################################### */
SET NOCOUNT ON;

INSERT INTO dbo.users (full_name, username, email, password_hash, role) VALUES
 (N'Nguyễn Quản Trị', N'admin',    N'admin@kho.local',    '$2b$12$REPLACE_WITH_REAL_BCRYPT_HASH_ADMIN',    'ADMIN'),
 (N'Trần Quản Lý',     N'manager',  N'manager@kho.local',  '$2b$12$REPLACE_WITH_REAL_BCRYPT_HASH_MANAGER',  'MANAGER'),
 (N'Lê Nhân Viên',     N'employee', N'employee@kho.local', '$2b$12$REPLACE_WITH_REAL_BCRYPT_HASH_EMPLOYEE', 'EMPLOYEE');

INSERT INTO dbo.categories (name) VALUES (N'Đồ uống'), (N'Bánh kẹo'), (N'Gia vị'), (N'Đồ gia dụng');   -- 1..4
INSERT INTO dbo.units (name)      VALUES (N'Lon'), (N'Chai'), (N'Gói'), (N'Hộp'), (N'Cái');             -- 1..5

INSERT INTO dbo.suppliers (name, phone, email, address) VALUES
 (N'Công ty TNHH Thực phẩm Bình An',  '0281234567', 'sales@binhan.example',  N'12 Nguyễn Huệ, Q.1, TP.HCM'),
 (N'Công ty CP Gia vị Việt',          '0287654321', 'order@giaviet.example', N'45 Lý Thường Kiệt, Q.10, TP.HCM'),
 (N'Nhà phân phối Gia dụng Tâm Phát', '0289998888', NULL,                    N'88 Cách Mạng Tháng 8, Q.3, TP.HCM');

-- Sản phẩm 1..8 (trigger tự tạo dòng inventory = 0)
INSERT INTO dbo.products (sku, barcode, name, category_id, unit_id, cost_price, export_price, minimum_stock, has_expiry) VALUES
 ('SP001', '8930000000011', N'Nước ngọt có ga lon 330ml', 1, 1,  7000,  9000, 100, 1),
 ('SP002', '8930000000028', N'Nước suối chai 500ml',       1, 2,  3000,  4500, 200, 1),
 ('SP003', '8930000000035', N'Bánh quy bơ hộp 400g',       2, 4, 35000, 45000,  30, 1),
 ('SP004', NULL,            N'Kẹo dẻo gói 200g',           2, 3, 12000, 16000,  50, 1),
 ('SP005', '8930000000059', N'Nước mắm chai 500ml',        3, 2, 28000, 36000,  40, 1),
 ('SP006', '8930000000066', N'Đường cát gói 1kg',          3, 3, 18000, 23000,  60, 1),
 ('SP007', NULL,            N'Khăn giấy hộp',              4, 4, 15000, 20000,  30, 0),
 ('SP008', NULL,            N'Ly nhựa dùng một lần',       4, 5,   500,   800, 500, 0);

DECLARE @today DATE = CAST(dbo.fn_now() AS DATE);
DECLARE @pn1 INT, @pn2 INT, @pn3 INT, @px1 INT, @px2 INT, @px3 INT, @px4 INT, @px5 INT, @kk1 INT;

-- PN-0001: nhập từ NCC 1, Employee lập, Manager duyệt => cộng tồn
INSERT INTO dbo.import_orders (code, supplier_id, created_by, import_date)
VALUES ('PN-0001', 1, 3, DATEADD(DAY, -30, @today));
SET @pn1 = CAST(SCOPE_IDENTITY() AS INT);

INSERT INTO dbo.import_order_details (import_order_id, product_id, quantity, unit_price, manufacturing_date, expiry_date) VALUES
 (@pn1, 1,  500,  7000, DATEADD(DAY,  -30, @today), DATEADD(DAY,  150, @today)),
 (@pn1, 2,  600,  3000, DATEADD(DAY,  -30, @today), DATEADD(DAY,  300, @today)),
 (@pn1, 3,  100, 35000, DATEADD(DAY, -160, @today), DATEADD(DAY,   20, @today)),   -- sắp hết hạn
 (@pn1, 4,   80, 12000, DATEADD(DAY, -200, @today), DATEADD(DAY,   -5, @today)),   -- đã hết hạn
 (@pn1, 7,  100, 15000, NULL, NULL),                                               -- không quản lý HSD
 (@pn1, 8, 1000,   500, NULL, NULL);

EXEC dbo.sp_submit_import_order  @id = @pn1, @user_id = 3;
EXEC dbo.sp_approve_import_order @id = @pn1, @user_id = 2;

-- PN-0002: đang Chờ duyệt (chưa cộng tồn)
INSERT INTO dbo.import_orders (code, supplier_id, created_by, import_date)
VALUES ('PN-0002', 2, 3, @today);
SET @pn2 = CAST(SCOPE_IDENTITY() AS INT);

INSERT INTO dbo.import_order_details (import_order_id, product_id, quantity, unit_price, manufacturing_date, expiry_date) VALUES
 (@pn2, 5, 120, 28000, @today, DATEADD(DAY, 365, @today)),
 (@pn2, 6, 200, 18000, @today, DATEADD(DAY, 400, @today));

EXEC dbo.sp_submit_import_order @id = @pn2, @user_id = 3;

-- PN-0003: còn Nháp
INSERT INTO dbo.import_orders (code, supplier_id, created_by, import_date)
VALUES ('PN-0003', 1, 3, @today);
SET @pn3 = CAST(SCOPE_IDENTITY() AS INT);

INSERT INTO dbo.import_order_details (import_order_id, product_id, quantity, unit_price, manufacturing_date, expiry_date)
VALUES (@pn3, 1, 200, 7000, @today, DATEADD(DAY, 180, @today));

-- PX-0001: xuất giao hàng đã duyệt (Employee lập, Manager duyệt)
INSERT INTO dbo.export_orders (code, type, destination, export_date, created_by)
VALUES ('PX-0001', 'DELIVERY', N'Cửa hàng tạp hóa Minh Anh - 12 Lê Lợi, Q.1', @today, 3);
SET @px1 = CAST(SCOPE_IDENTITY() AS INT);

EXEC dbo.sp_add_export_item @order_id = @px1, @product_id = 1, @quantity = 100, @unit_price = NULL, @user_id = 3;
EXEC dbo.sp_add_export_item @order_id = @px1, @product_id = 2, @quantity = 150, @unit_price = NULL, @user_id = 3;
EXEC dbo.sp_add_export_item @order_id = @px1, @product_id = 7, @quantity = 20,  @unit_price = NULL, @user_id = 3;
EXEC dbo.sp_submit_export_order  @id = @px1, @user_id = 3;
EXEC dbo.sp_approve_export_order @id = @px1, @user_id = 2;

-- PX-0002: xuất hư hỏng (Manager lập, Admin duyệt)
INSERT INTO dbo.export_orders (code, type, export_date, created_by, reason)
VALUES ('PX-0002', 'DAMAGE', @today, 2, N'Hộp bị móp, rách bao bì');
SET @px2 = CAST(SCOPE_IDENTITY() AS INT);

EXEC dbo.sp_add_export_item @order_id = @px2, @product_id = 3, @quantity = 5, @unit_price = NULL, @user_id = 2;
EXEC dbo.sp_submit_export_order  @id = @px2, @user_id = 2;
EXEC dbo.sp_approve_export_order @id = @px2, @user_id = 1;

-- PX-0003: xuất hết hạn (một phần số lượng của SP004)
INSERT INTO dbo.export_orders (code, type, export_date, created_by, reason)
VALUES ('PX-0003', 'EXPIRED', @today, 2, N'Hết hạn sử dụng, tiêu hủy theo kiểm tra thực tế');
SET @px3 = CAST(SCOPE_IDENTITY() AS INT);

EXEC dbo.sp_add_export_item @order_id = @px3, @product_id = 4, @quantity = 30, @unit_price = NULL, @user_id = 2;
EXEC dbo.sp_submit_export_order  @id = @px3, @user_id = 2;
EXEC dbo.sp_approve_export_order @id = @px3, @user_id = 1;

-- PX-0004: xuất giao hàng đang Chờ duyệt
INSERT INTO dbo.export_orders (code, type, destination, export_date, created_by)
VALUES ('PX-0004', 'DELIVERY', N'Chuỗi bán lẻ ABC - Chi nhánh Thủ Đức', @today, 3);
SET @px4 = CAST(SCOPE_IDENTITY() AS INT);

EXEC dbo.sp_add_export_item @order_id = @px4, @product_id = 1, @quantity = 50, @unit_price = NULL, @user_id = 3;
EXEC dbo.sp_submit_export_order @id = @px4, @user_id = 3;

-- PX-0005: xuất giao hàng còn Nháp
INSERT INTO dbo.export_orders (code, type, destination, export_date, created_by)
VALUES ('PX-0005', 'DELIVERY', N'Đại lý Hòa Phát - Bình Dương', @today, 3);
SET @px5 = CAST(SCOPE_IDENTITY() AS INT);

EXEC dbo.sp_add_export_item @order_id = @px5, @product_id = 8, @quantity = 100, @unit_price = NULL, @user_id = 3;

-- KK-0001: kiểm kê nhóm Đồ uống; Employee nhập thực tế, đang Chờ duyệt
EXEC dbo.sp_create_stock_take @code = 'KK-0001', @user_id = 3, @category_id = 1, @stock_take_id = @kk1 OUTPUT;

UPDATE dbo.stock_take_details SET actual_quantity = system_quantity WHERE stock_take_id = @kk1;
UPDATE dbo.stock_take_details SET actual_quantity = system_quantity - 2          -- lệch -2 ở SP001
 WHERE stock_take_id = @kk1 AND product_id = 1;

EXEC dbo.sp_submit_stock_take @id = @kk1, @user_id = 3;

-- Audit log mẫu
INSERT INTO dbo.audit_logs (user_id, action, module, reference_id, metadata) VALUES
 (1, 'LOGIN', 'AUTH', NULL, N'{"ip":"127.0.0.1"}'),
 (2, 'LOGIN', 'AUTH', NULL, N'{"ip":"127.0.0.1"}'),
 (3, 'LOGIN', 'AUTH', NULL, N'{"ip":"127.0.0.1"}');
GO

PRINT N'=== DỰNG DATABASE WAREHOUSE_DB THÀNH CÔNG ===';
GO