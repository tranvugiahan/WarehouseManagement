using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace WarehouseManagement.Models;

public partial class WarehouseDbContext : DbContext
{
    public WarehouseDbContext()
    {
    }

    public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<ExportOrder> ExportOrders { get; set; }

    public virtual DbSet<ExportOrderDetail> ExportOrderDetails { get; set; }

    public virtual DbSet<ImportOrder> ImportOrders { get; set; }

    public virtual DbSet<ImportOrderDetail> ImportOrderDetails { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<StatusHistory> StatusHistories { get; set; }

    public virtual DbSet<StockMovement> StockMovements { get; set; }

    public virtual DbSet<StockTake> StockTakes { get; set; }

    public virtual DbSet<StockTakeDetail> StockTakeDetails { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<Unit> Units { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VDamageExpiredReport> VDamageExpiredReports { get; set; }

    public virtual DbSet<VExpiryAlert> VExpiryAlerts { get; set; }

    public virtual DbSet<VExportByDestination> VExportByDestinations { get; set; }

    public virtual DbSet<VExportOrderTotal> VExportOrderTotals { get; set; }

    public virtual DbSet<VImportOrderTotal> VImportOrderTotals { get; set; }

    public virtual DbSet<VInventoryStatus> VInventoryStatuses { get; set; }

    public virtual DbSet<VLowStockAlert> VLowStockAlerts { get; set; }

    public virtual DbSet<VStockMovementHistory> VStockMovementHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Vietnamese_CI_AS");

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_audit_logs");

            entity.ToTable("audit_logs", tb => tb.HasTrigger("trg_al_block"));

            entity.HasIndex(e => new { e.Module, e.CreatedAt }, "idx_al_module_time");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "idx_al_user_time");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Action)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("action");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("([dbo].[fn_now]())")
                .HasColumnName("created_at");
            entity.Property(e => e.Metadata).HasColumnName("metadata");
            entity.Property(e => e.Module)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("module");
            entity.Property(e => e.ReferenceId).HasColumnName("reference_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_al_user");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_categories");

            entity.ToTable("categories");

            entity.HasIndex(e => e.Name, "uq_categories_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVE")
                .HasColumnName("status");
        });

        modelBuilder.Entity<ExportOrder>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_export_orders");

            entity.ToTable("export_orders", tb =>
                {
                    tb.HasTrigger("trg_eo_ad");
                    tb.HasTrigger("trg_eo_ai");
                    tb.HasTrigger("trg_eo_au");
                });

            entity.HasIndex(e => e.ApprovedBy, "idx_eo_approved_by");

            entity.HasIndex(e => e.CreatedBy, "idx_eo_created_by");

            entity.HasIndex(e => e.Destination, "idx_eo_destination");

            entity.HasIndex(e => new { e.Type, e.Status, e.ExportDate }, "idx_eo_type_status_date");

            entity.HasIndex(e => e.Code, "uq_export_orders_code").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("([dbo].[fn_now]())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Destination)
                .HasMaxLength(255)
                .HasColumnName("destination");
            entity.Property(e => e.ExportDate).HasColumnName("export_date");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .HasColumnName("reason");
            entity.Property(e => e.RejectionReason)
                .HasMaxLength(500)
                .HasColumnName("rejection_reason");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("DRAFT")
                .HasColumnName("status");
            entity.Property(e => e.Type)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("type");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.ExportOrderApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("fk_eo_approved_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ExportOrderCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_eo_created_by");
        });

        modelBuilder.Entity<ExportOrderDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_export_order_details");

            entity.ToTable("export_order_details", tb => tb.HasTrigger("trg_eod_iud"));

            entity.HasIndex(e => e.ExportOrderId, "idx_eod_order");

            entity.HasIndex(e => e.ProductId, "idx_eod_product");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ExportOrderId).HasColumnName("export_order_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("unit_price");

            entity.HasOne(d => d.ExportOrder).WithMany(p => p.ExportOrderDetails)
                .HasForeignKey(d => d.ExportOrderId)
                .HasConstraintName("fk_eod_order");

            entity.HasOne(d => d.Product).WithMany(p => p.ExportOrderDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_eod_product");
        });

        modelBuilder.Entity<ImportOrder>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_import_orders");

            entity.ToTable("import_orders", tb =>
                {
                    tb.HasTrigger("trg_io_ad");
                    tb.HasTrigger("trg_io_au");
                });

            entity.HasIndex(e => e.ApprovedBy, "idx_io_approved_by");

            entity.HasIndex(e => e.CreatedBy, "idx_io_created_by");

            entity.HasIndex(e => new { e.Status, e.ImportDate }, "idx_io_status_date");

            entity.HasIndex(e => e.SupplierId, "idx_io_supplier");

            entity.HasIndex(e => e.Code, "uq_import_orders_code").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("([dbo].[fn_now]())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.ImportDate).HasColumnName("import_date");
            entity.Property(e => e.RejectionReason)
                .HasMaxLength(500)
                .HasColumnName("rejection_reason");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("DRAFT")
                .HasColumnName("status");
            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.ImportOrderApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("fk_io_approved_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ImportOrderCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_io_created_by");

            entity.HasOne(d => d.Supplier).WithMany(p => p.ImportOrders)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_io_supplier");
        });

        modelBuilder.Entity<ImportOrderDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_import_order_details");

            entity.ToTable("import_order_details", tb => tb.HasTrigger("trg_iod_iud"));

            entity.HasIndex(e => e.ExpiryDate, "idx_iod_expiry");

            entity.HasIndex(e => e.ImportOrderId, "idx_iod_order");

            entity.HasIndex(e => e.ProductId, "idx_iod_product");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            entity.Property(e => e.ImportOrderId).HasColumnName("import_order_id");
            entity.Property(e => e.ManufacturingDate).HasColumnName("manufacturing_date");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("unit_price");

            entity.HasOne(d => d.ImportOrder).WithMany(p => p.ImportOrderDetails)
                .HasForeignKey(d => d.ImportOrderId)
                .HasConstraintName("fk_iod_order");

            entity.HasOne(d => d.Product).WithMany(p => p.ImportOrderDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_iod_product");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("pk_inventory");

            entity.ToTable("inventory");

            entity.Property(e => e.ProductId)
                .ValueGeneratedNever()
                .HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("([dbo].[fn_now]())")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Product).WithOne(p => p.Inventory)
                .HasForeignKey<Inventory>(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_inventory_product");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_products");

            entity.ToTable("products", tb => tb.HasTrigger("trg_products_ai"));

            entity.HasIndex(e => e.CategoryId, "idx_products_category");

            entity.HasIndex(e => e.Name, "idx_products_name");

            entity.HasIndex(e => e.UnitId, "idx_products_unit");

            entity.HasIndex(e => e.Barcode, "uq_products_barcode")
                .IsUnique()
                .HasFilter("([barcode] IS NOT NULL)");

            entity.HasIndex(e => e.Sku, "uq_products_sku").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Barcode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("barcode");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CostPrice)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("cost_price");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("([dbo].[fn_now]())")
                .HasColumnName("created_at");
            entity.Property(e => e.ExportPrice)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("export_price");
            entity.Property(e => e.HasExpiry).HasColumnName("has_expiry");
            entity.Property(e => e.MinimumStock).HasColumnName("minimum_stock");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.Sku)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("sku");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVE")
                .HasColumnName("status");
            entity.Property(e => e.UnitId).HasColumnName("unit_id");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_products_category");

            entity.HasOne(d => d.Unit).WithMany(p => p.Products)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_products_unit");
        });

        modelBuilder.Entity<StatusHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_status_history");

            entity.ToTable("status_history", tb => tb.HasTrigger("trg_sh_block"));

            entity.HasIndex(e => e.ChangedBy, "idx_sh_changed_by");

            entity.HasIndex(e => new { e.DocumentType, e.DocumentId }, "idx_sh_document");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChangedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("([dbo].[fn_now]())")
                .HasColumnName("changed_at");
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by");
            entity.Property(e => e.DocumentId).HasColumnName("document_id");
            entity.Property(e => e.DocumentType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("document_type");
            entity.Property(e => e.NewStatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("new_status");
            entity.Property(e => e.OldStatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("old_status");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .HasColumnName("reason");

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.StatusHistories)
                .HasForeignKey(d => d.ChangedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_sh_changed_by");
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_stock_movements");

            entity.ToTable("stock_movements", tb => tb.HasTrigger("trg_sm_block"));

            entity.HasIndex(e => e.CreatedBy, "idx_sm_created_by");

            entity.HasIndex(e => new { e.ProductId, e.CreatedAt }, "idx_sm_product_time");

            entity.HasIndex(e => new { e.ReferenceType, e.ReferenceId }, "idx_sm_reference");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("([dbo].[fn_now]())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.QuantityAfter).HasColumnName("quantity_after");
            entity.Property(e => e.QuantityBefore).HasColumnName("quantity_before");
            entity.Property(e => e.ReferenceId).HasColumnName("reference_id");
            entity.Property(e => e.ReferenceType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("reference_type");
            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("type");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_sm_created_by");

            entity.HasOne(d => d.Product).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_sm_product");
        });

        modelBuilder.Entity<StockTake>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_stock_takes");

            entity.ToTable("stock_takes", tb =>
                {
                    tb.HasTrigger("trg_st_ad");
                    tb.HasTrigger("trg_st_au");
                });

            entity.HasIndex(e => e.ApprovedBy, "idx_st_approved_by");

            entity.HasIndex(e => e.CreatedBy, "idx_st_created_by");

            entity.HasIndex(e => e.Status, "idx_st_status");

            entity.HasIndex(e => e.Code, "uq_stock_takes_code").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("([dbo].[fn_now]())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.RejectionReason)
                .HasMaxLength(500)
                .HasColumnName("rejection_reason");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("DRAFT")
                .HasColumnName("status");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.StockTakeApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("fk_st_approved_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.StockTakeCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_st_created_by");
        });

        modelBuilder.Entity<StockTakeDetail>(entity =>
        {
            entity.HasKey(e => new { e.StockTakeId, e.ProductId }).HasName("pk_stock_take_details");

            entity.ToTable("stock_take_details", tb => tb.HasTrigger("trg_std_iud"));

            entity.HasIndex(e => e.ProductId, "idx_std_product");

            entity.Property(e => e.StockTakeId).HasColumnName("stock_take_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ActualQuantity).HasColumnName("actual_quantity");
            entity.Property(e => e.Difference)
                .HasComputedColumnSql("([actual_quantity]-[system_quantity])", true)
                .HasColumnName("difference");
            entity.Property(e => e.SystemQuantity).HasColumnName("system_quantity");

            entity.HasOne(d => d.Product).WithMany(p => p.StockTakeDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_std_product");

            entity.HasOne(d => d.StockTake).WithMany(p => p.StockTakeDetails)
                .HasForeignKey(d => d.StockTakeId)
                .HasConstraintName("fk_std_stock_take");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_suppliers");

            entity.ToTable("suppliers");

            entity.HasIndex(e => e.Name, "idx_suppliers_name");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVE")
                .HasColumnName("status");
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_units");

            entity.ToTable("units");

            entity.HasIndex(e => e.Name, "uq_units_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVE")
                .HasColumnName("status");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_users");

            entity.ToTable("users", tb => tb.HasTrigger("trg_users_au"));

            entity.HasIndex(e => e.Email, "uq_users_email").IsUnique();

            entity.HasIndex(e => e.Username, "uq_users_username").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("([dbo].[fn_now]())")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.LastLoginAt)
                .HasPrecision(0)
                .HasColumnName("last_login_at");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password_hash");
            entity.Property(e => e.Role)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("EMPLOYEE")
                .HasColumnName("role");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVE")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("([dbo].[fn_now]())")
                .HasColumnName("updated_at");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");
        });

        modelBuilder.Entity<VDamageExpiredReport>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_damage_expired_report");

            entity.Property(e => e.Amount)
                .HasColumnType("decimal(26, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.ExportDate).HasColumnName("export_date");
            entity.Property(e => e.ExportOrderId).HasColumnName("export_order_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ProductName)
                .HasMaxLength(200)
                .HasColumnName("product_name");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .HasColumnName("reason");
            entity.Property(e => e.Sku)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("sku");
            entity.Property(e => e.Type)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("type");
        });

        modelBuilder.Entity<VExpiryAlert>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_expiry_alerts");

            entity.Property(e => e.CurrentStock).HasColumnName("current_stock");
            entity.Property(e => e.DaysToExpiry).HasColumnName("days_to_expiry");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            entity.Property(e => e.ImportCode)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("import_code");
            entity.Property(e => e.ImportDate).HasColumnName("import_date");
            entity.Property(e => e.ImportDetailId).HasColumnName("import_detail_id");
            entity.Property(e => e.ImportOrderId).HasColumnName("import_order_id");
            entity.Property(e => e.ImportedQuantity).HasColumnName("imported_quantity");
            entity.Property(e => e.ManufacturingDate).HasColumnName("manufacturing_date");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ProductName)
                .HasMaxLength(200)
                .HasColumnName("product_name");
            entity.Property(e => e.Sku)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("sku");
        });

        modelBuilder.Entity<VExportByDestination>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_export_by_destination");

            entity.Property(e => e.Destination)
                .HasMaxLength(255)
                .HasColumnName("destination");
            entity.Property(e => e.ExportDate).HasColumnName("export_date");
            entity.Property(e => e.ExportMonth)
                .HasMaxLength(7)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("export_month");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ProductName)
                .HasMaxLength(200)
                .HasColumnName("product_name");
            entity.Property(e => e.Sku)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("sku");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(38, 2)")
                .HasColumnName("total_amount");
            entity.Property(e => e.TotalQuantity).HasColumnName("total_quantity");
        });

        modelBuilder.Entity<VExportOrderTotal>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_export_order_totals");

            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.Destination)
                .HasMaxLength(255)
                .HasColumnName("destination");
            entity.Property(e => e.ExportDate).HasColumnName("export_date");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LineCount).HasColumnName("line_count");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(38, 2)")
                .HasColumnName("total_amount");
            entity.Property(e => e.TotalQuantity).HasColumnName("total_quantity");
            entity.Property(e => e.Type)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("type");
        });

        modelBuilder.Entity<VImportOrderTotal>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_import_order_totals");

            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ImportDate).HasColumnName("import_date");
            entity.Property(e => e.LineCount).HasColumnName("line_count");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
            entity.Property(e => e.SupplierName)
                .HasMaxLength(150)
                .HasColumnName("supplier_name");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(38, 2)")
                .HasColumnName("total_amount");
            entity.Property(e => e.TotalQuantity).HasColumnName("total_quantity");
        });

        modelBuilder.Entity<VInventoryStatus>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_inventory_status");

            entity.Property(e => e.Barcode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("barcode");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(100)
                .HasColumnName("category_name");
            entity.Property(e => e.CostPrice)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("cost_price");
            entity.Property(e => e.IsLowStock).HasColumnName("is_low_stock");
            entity.Property(e => e.MinimumStock).HasColumnName("minimum_stock");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ProductName)
                .HasMaxLength(200)
                .HasColumnName("product_name");
            entity.Property(e => e.ProductStatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("product_status");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Sku)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("sku");
            entity.Property(e => e.StockValue)
                .HasColumnType("decimal(34, 4)")
                .HasColumnName("stock_value");
            entity.Property(e => e.UnitName)
                .HasMaxLength(50)
                .HasColumnName("unit_name");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<VLowStockAlert>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_low_stock_alerts");

            entity.Property(e => e.Barcode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("barcode");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(100)
                .HasColumnName("category_name");
            entity.Property(e => e.CostPrice)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("cost_price");
            entity.Property(e => e.IsLowStock).HasColumnName("is_low_stock");
            entity.Property(e => e.MinimumStock).HasColumnName("minimum_stock");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ProductName)
                .HasMaxLength(200)
                .HasColumnName("product_name");
            entity.Property(e => e.ProductStatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("product_status");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Sku)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("sku");
            entity.Property(e => e.StockValue)
                .HasColumnType("decimal(34, 4)")
                .HasColumnName("stock_value");
            entity.Property(e => e.UnitName)
                .HasMaxLength(50)
                .HasColumnName("unit_name");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<VStockMovementHistory>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_stock_movement_history");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedByName)
                .HasMaxLength(100)
                .HasColumnName("created_by_name");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ProductName)
                .HasMaxLength(200)
                .HasColumnName("product_name");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.QuantityAfter).HasColumnName("quantity_after");
            entity.Property(e => e.QuantityBefore).HasColumnName("quantity_before");
            entity.Property(e => e.ReferenceId).HasColumnName("reference_id");
            entity.Property(e => e.ReferenceType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("reference_type");
            entity.Property(e => e.Sku)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("sku");
            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("type");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
