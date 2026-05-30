using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMS.Common;

namespace WMS.Models;

[Table("Items")]
public class Item : IOwnerScoped
{
    [Key]
    public int ItemId { get; set; }

    [Required, MaxLength(50)]
    public string ItemCode { get; set; } = "";

    [Required, MaxLength(200)]
    public string ItemName { get; set; } = "";

    [MaxLength(100)]
    public string? Barcode { get; set; }

    [MaxLength(50)]
    public string? SkuCode { get; set; }

    public int? CategoryId { get; set; }

    public int? OwnerPartnerId { get; set; }

    public ItemTypeEnum ItemType { get; set; } = ItemTypeEnum.NguyenVatLieu;

    public int BaseUomId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal CurrentStock { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal MinThreshold { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? MaxThreshold { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? ReorderPoint { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? Weight { get; set; } // Khối lượng đơn vị (kg) - dùng để tính sức chứa ô kho

    // Kích thước đơn vị (cm) — theo đặc tả 6.1: D×R×C
    [Column(TypeName = "decimal(8,2)")]
    public decimal? Length { get; set; } // Dài (cm)

    [Column(TypeName = "decimal(8,2)")]
    public decimal? Width { get; set; } // Rộng (cm)

    [Column(TypeName = "decimal(8,2)")]
    public decimal? Height { get; set; } // Cao (cm)

    // Tracking flags — theo đặc tả 6.1: cấu hình theo từng SKU
    public bool TrackExpiry { get; set; } // Quản lý hạn sử dụng? → bắt buộc nhập ExpiryDate khi nhập kho
    public bool TrackLot { get; set; } // Quản lý theo lô? → bắt buộc nhập LotNumber khi nhập kho
    public bool TrackSerial { get; set; } // Quản lý theo serial? → bắt buộc đăng ký serial trước khi hoàn tất inbound
    public bool TrackCatchWeight { get; set; }
    public int? CatchWeightUomId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? NominalWeightPerBaseUnit { get; set; }

    [Column(TypeName = "decimal(9,4)")]
    public decimal? CatchWeightTolerancePercent { get; set; }

    public bool RequireCatchWeightAtReceive { get; set; }
    public bool RequireCatchWeightAtPickPack { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitCost { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? LastCost { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalStockValue { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public string? Specifications { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = VietnamTime.Now;

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    [ForeignKey("CategoryId")]
    public ItemCategory? Category { get; set; }

    [ForeignKey(nameof(OwnerPartnerId))]
    public Partner? OwnerPartner { get; set; }

    [ForeignKey("BaseUomId")]
    public UnitOfMeasure BaseUom { get; set; } = null!;

    [ForeignKey(nameof(CatchWeightUomId))]
    public UnitOfMeasure? CatchWeightUom { get; set; }

    /// <summary>Chiến lược put-away tự động (Default / NearestEmpty / Consolidate)</summary>
    public PutawayStrategyEnum PutawayStrategy { get; set; } = PutawayStrategyEnum.Default;

    /// <summary>Zone types cho phép (CSV, ví dụ "1,4"). Null = tất cả.</summary>
    [MaxLength(50)]
    public string? AllowedZoneTypes { get; set; }

    /// <summary>Phân hạng ABC (A=xoay nhanh, B=trung bình, C=xoay chậm). Null = chưa phân hạng.</summary>
    [MaxLength(1)]
    public string? AbcClass { get; set; }

    public int? DefaultLocationId { get; set; }

    [ForeignKey("DefaultLocationId")]
    public Location? DefaultLocation { get; set; }

    public string ItemTypeName => ItemType switch
    {
        ItemTypeEnum.NguyenVatLieu => "Nguyên Vật Liệu",
        ItemTypeEnum.ThanhPham => "Thành Phẩm",
        ItemTypeEnum.BanThanhPham => "Bán Thành Phẩm",
        ItemTypeEnum.PhuTung => "Phụ Tùng / Linh Kiện",
        ItemTypeEnum.BaoBi => "Bao Bì",
        ItemTypeEnum.HoaChat => "Hóa Chất",
        ItemTypeEnum.HangMau => "Hàng Mẫu / QC",
        _ => "Khác"
    };

    public string StockStatus => CurrentStock switch
    {
        <= 0 => "Hết Hàng",
        _ when CurrentStock <= MinThreshold => "Sắp Hết",
        _ when MaxThreshold.HasValue && CurrentStock >= MaxThreshold.Value => "Vượt Định Mức",
        _ => "Bình Thường"
    };
}
