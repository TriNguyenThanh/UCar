using System.ComponentModel.DataAnnotations;

namespace UCar.Models.DTOs.Operations;

// ============ Branch DTOs ============

public class BranchDto
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? PhoneContact { get; set; }
    public int StaffCount { get; set; }
    public int VehicleCount { get; set; }
    public Guid? ManagerId { get; set; }
    public string? ManagerName { get; set; }
}

public class BranchCreateDto
{
    [Required(ErrorMessage = "Vui lòng nhập tên chi nhánh")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(20)]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [RegularExpression(@"^\d{10,11}$", ErrorMessage = "Số điện thoại phải có 10 hoặc 11 số")]
    public string? PhoneContact { get; set; }
}

public class BranchUpdateDto : BranchCreateDto
{
}
