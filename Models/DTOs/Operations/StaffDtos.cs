using System.ComponentModel.DataAnnotations;
using UCar.Models.Enums;

namespace UCar.Models.DTOs.Operations;

// ============ Staff DTOs ============

public class StaffListDto
{
    public Guid StaffId { get; set; }
    public string StaffCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public bool IsActive { get; set; }
    public int TasksCompleted { get; set; }
}

public class StaffDetailDto : StaffListDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StaffCreateDto
{
    [Required(ErrorMessage = "Vui lòng nhập mã nhân viên")]
    [MaxLength(50)]
    public string StaffCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Position { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn chi nhánh")]
    public Guid BranchId { get; set; }

    // Account info
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    public string Password { get; set; } = string.Empty;

    [MaxLength(200)]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string? Email { get; set; }

    [MaxLength(20)]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [RegularExpression(@"^0\d{9,}$", ErrorMessage = "SĐT phải bắt đầu bằng 0 và có tối thiểu 10 số")]
    public string? Phone { get; set; }
}

public class StaffUpdateDto
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Position { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn chi nhánh")]
    public Guid BranchId { get; set; }

    [MaxLength(200)]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string? Email { get; set; }

    [MaxLength(20)]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [RegularExpression(@"^0\d{9,}$", ErrorMessage = "SĐT phải bắt đầu bằng 0 và có tối thiểu 10 số")]
    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;
}

public class StaffFilterDto
{
    public string? Search { get; set; }
    public Guid? BranchId { get; set; }
    public string? Position { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class StaffPerformanceDto
{
    public Guid StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int FailedTasks { get; set; }
    public int CancelledTasks { get; set; }
    public double CompletionRate { get; set; }
    public double OnTimeRate { get; set; }
    public int TotalShiftsWorked { get; set; }
}
