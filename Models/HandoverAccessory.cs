using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UCar.Models;

/// <summary>
/// Phụ kiện bàn giao theo xe (chìa khóa, cavet, thẻ gửi xe, camera hành trình...)
/// </summary>
public class HandoverAccessory
{
    [Key]
    public Guid AccessoryId { get; set; }

    /// <summary>Biên bản giao xe (khi giao)</summary>
    public Guid? HandoverId { get; set; }

    /// <summary>Biên bản nhận xe (khi trả)</summary>
    public Guid? ReturnId { get; set; }

    /// <summary>Tên phụ kiện</summary>
    [Required]
    [MaxLength(100)]
    public string AccessoryName { get; set; } = string.Empty;

    /// <summary>Số lượng</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>Đã trả đúng số lượng/tình trạng</summary>
    public bool IsReturnedOk { get; set; } = true;

    /// <summary>Ghi chú hư hỏng/thiếu</summary>
    [MaxLength(500)]
    public string? DamageNote { get; set; }

    /// <summary>Giá trị ước tính (để tính phí nếu mất)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal EstimatedValue { get; set; }

    // Navigation properties
    [ForeignKey(nameof(HandoverId))]
    public HandoverRecord? HandoverRecord { get; set; }

    [ForeignKey(nameof(ReturnId))]
    public ReturnRecord? ReturnRecord { get; set; }
}
