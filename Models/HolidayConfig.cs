using System.ComponentModel.DataAnnotations;

namespace UCar.Models;

/// <summary>
/// Cấu hình ngày lễ để áp dụng HolidayMultiplier trong tính giá
/// </summary>
public class HolidayConfig
{
    [Key]
    public Guid HolidayId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Ngày lễ cụ thể (cho lễ dương lịch)
    /// </summary>
    public DateTime? Date { get; set; }

    /// <summary>
    /// Ngày theo âm lịch (dạng text: "01/01" cho mùng 1 Tết, "10/03" cho Giỗ Tổ)
    /// </summary>
    [MaxLength(10)]
    public string? LunarDate { get; set; }

    /// <summary>
    /// Ngày bắt đầu (cho kỳ nghỉ dài ngày như Tết)
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Ngày kết thúc (cho kỳ nghỉ dài ngày)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Lặp lại hàng năm (true cho các ngày lễ cố định)
    /// </summary>
    public bool IsRecurring { get; set; } = true;

    /// <summary>
    /// Mô tả chi tiết
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Kích hoạt áp dụng cho tính giá
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}
