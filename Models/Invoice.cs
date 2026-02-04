using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCar.Models.Enums;

namespace UCar.Models
{
    /// <summary>
    /// Hóa đơn - Theo dõi các khoản thanh toán trong từng giai đoạn
    /// </summary>
    public class Invoice
    {
        [Key]
        public Guid InvoiceId { get; set; }

        /// <summary>
        /// Mã hóa đơn (DEP-2601-0001, RNT-2601-0001, SUR-2601-0001, REF-2601-0001)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string InvoiceNumber { get; set; } = string.Empty;

        /// <summary>
        /// Loại hóa đơn (Deposit, Rental, Surcharge, Penalty, Refund)
        /// </summary>
        [Required]
        public InvoiceType InvoiceType { get; set; }

        /// <summary>
        /// Hợp đồng liên quan
        /// </summary>
        [Required]
        public Guid ContractId { get; set; }
        [ForeignKey(nameof(ContractId))]
        public RentalContract Contract { get; set; } = null!;

        /// <summary>
        /// Khách hàng
        /// </summary>
        [Required]
        public Guid CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public Customer Customer { get; set; } = null!;

        /// <summary>
        /// Ngày phát hành
        /// </summary>
        [Required]
        public DateTime IssuedDate { get; set; }

        /// <summary>
        /// Hạn thanh toán
        /// </summary>
        public DateTime? DueDate { get; set; }

        // ===== BREAKDOWN AMOUNTS =====
        /// <summary>
        /// Tiền thuê cơ bản (chỉ cho Rental Invoice)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseRentalAmount { get; set; }

        /// <summary>
        /// Tổng phụ phí (chỉ cho Surcharge Invoice)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal SurchargesTotal { get; set; }

        /// <summary>
        /// Tổng phạt (chỉ cho Penalty Invoice)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal PenaltiesTotal { get; set; }

        /// <summary>
        /// Giảm giá
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        /// <summary>
        /// Thuế
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        /// <summary>
        /// Cọc đã thanh toán (để hiển thị trong Rental Invoice)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal DepositPaid { get; set; }

        // ===== FINAL AMOUNTS =====
        /// <summary>
        /// Tổng tiền hóa đơn
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Đã thanh toán
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }

        /// <summary>
        /// Còn phải trả
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountDue { get; set; }

        // ===== STATUS & TRACKING =====
        /// <summary>
        /// Trạng thái hóa đơn
        /// </summary>
        [Required]
        public InvoiceStatus Status { get; set; }

        /// <summary>
        /// Người dùng phát hành (UserAccount - Staff)
        /// </summary>
        [Required]
        public Guid IssuedBy { get; set; }
        [ForeignKey(nameof(IssuedBy))]
        public UserAccount IssuedByUser { get; set; } = null!;

        /// <summary>
        /// Ghi chú hiển thị cho khách hàng
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// Ghi chú nội bộ
        /// </summary>
        [MaxLength(1000)]
        public string? InternalNotes { get; set; }

        /// <summary>
        /// Ngày tạo
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Ngày cập nhật
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        // ===== NAVIGATION PROPERTIES =====
        /// <summary>
        /// Chi tiết các dòng trong hóa đơn
        /// </summary>
        public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();

        /// <summary>
        /// Lịch sử điều chỉnh hóa đơn
        /// </summary>
        public ICollection<InvoiceAdjustment> Adjustments { get; set; } = new List<InvoiceAdjustment>();

        /// <summary>
        /// Các giao dịch thanh toán liên quan đến hóa đơn này
        /// </summary>
        public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
    }
}
