using UCar.Models.Enums;

namespace UCar.Models.DTOs.Vehicle;

/// <summary>
/// DTO cho lọc/tìm kiếm xe
/// </summary>
public class VehicleFilterDto
{
    /// <summary>Tìm theo biển số hoặc tên xe</summary>
    public string? SearchTerm { get; set; }

    /// <summary>Lọc theo loại xe</summary>
    public Guid? VehicleTypeId { get; set; }

    /// <summary>Lọc theo hãng xe</summary>
    public string? Make { get; set; }

    /// <summary>Lọc theo trạng thái</summary>
    public VehicleStatus? Status { get; set; }

    /// <summary>Lọc theo chi nhánh</summary>
    public Guid? BranchId { get; set; }

    /// <summary>Năm sản xuất từ</summary>
    public int? YearFrom { get; set; }

    /// <summary>Năm sản xuất đến</summary>
    public int? YearTo { get; set; }

    /// <summary>Số bản ghi mỗi trang</summary>
    public int PageSize { get; set; } = 10;

    /// <summary>Trang hiện tại (1-indexed)</summary>
    public int Page { get; set; } = 1;

    /// <summary>Cột sắp xếp</summary>
    public string? SortBy { get; set; }

    /// <summary>Hướng sắp xếp (asc/desc)</summary>
    public string SortDirection { get; set; } = "asc";
}

/// <summary>
/// Kết quả phân trang
/// </summary>
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
