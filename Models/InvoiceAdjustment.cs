using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCar.Models.Enums;

namespace UCar.Models
{
    /// <summary>
    /// Lịch sử điều chỉnh hóa đơn (audit trail)
    /// </summary>
    public class InvoiceAdjustment
    {
        [Key]
        public Guid AdjustmentId { get; set; }

        /// <summary>
        /// Hóa đơn được điều chỉnh
        /// </summary>
        [Required]
        public Guid InvoiceId { get; set; }
        [ForeignKey(nameof(InvoiceId))]
        public Invoice Invoice { get; set; } = null!;

        /// <summary>
        /// Loại điều chỉnh
        /// </summary>
        [Required]
        public AdjustmentType AdjustmentType { get; set; }

        /// <summary>
        /// Số tiền điều chỉnh (có thể âm hoặc dương)
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Lý do điều chỉnh
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Nhân viên thực hiện điều chỉnh
        /// </summary>
        [Required]
        public Guid AdjustedBy { get; set; }
        [ForeignKey(nameof(AdjustedBy))]
        public StaffProfile AdjustedByStaff { get; set; } = null!;

        /// <summary>
        /// Ngày điều chỉnh
        /// </summary>
        [Required]
        public DateTime AdjustedAt { get; set; }

        /// <summary>
        /// Ghi chú
        /// </summary>
        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
