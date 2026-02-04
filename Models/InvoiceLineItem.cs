using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCar.Models.Enums;

namespace UCar.Models
{
    /// <summary>
    /// Chi tiết từng dòng trong hóa đơn
    /// </summary>
    public class InvoiceLineItem
    {
        [Key]
        public Guid LineItemId { get; set; }

        /// <summary>
        /// Hóa đơn
        /// </summary>
        [Required]
        public Guid InvoiceId { get; set; }
        [ForeignKey(nameof(InvoiceId))]
        public Invoice Invoice { get; set; } = null!;

        /// <summary>
        /// Loại dòng chi tiết
        /// </summary>
        [Required]
        public LineItemType ItemType { get; set; }

        /// <summary>
        /// Mô tả
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Số lượng
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Đơn vị (ngày, giờ, km, lần)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// Đơn giá
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Thành tiền (Quantity × UnitPrice)
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Tham chiếu đến entity liên quan (ContractChargeId, ViolationId, etc.)
        /// </summary>
        public Guid? ReferenceId { get; set; }

        /// <summary>
        /// Ghi chú
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// Ngày tạo
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
