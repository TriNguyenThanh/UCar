using UCar.Models.Enums;
using UCar.ViewModels.Contract;

namespace UCar.Interfaces;

/// <summary>
/// Interface Service quản lý hợp đồng thuê xe
/// Tương ứng DFD 5.0: Quản lý hợp đồng thuê
/// </summary>
public interface IContractService
{
    // ===== 5.1 Tạo và quản lý hợp đồng thuê =====
    
    /// <summary>
    /// Lấy danh sách hợp đồng với filter và phân trang
    /// </summary>
    Task<ContractListResultViewModel> GetContractsAsync(ContractSearchViewModel filter);
    
    /// <summary>
    /// Lấy danh sách hợp đồng của khách hàng
    /// </summary>
    Task<ContractListResultViewModel> GetMyContractsAsync(Guid userId, ContractSearchViewModel filter);
    
    /// <summary>
    /// Lấy chi tiết hợp đồng
    /// </summary>
    Task<ContractDetailsViewModel?> GetContractDetailsAsync(Guid contractId);
    
    /// <summary>
    /// Lấy thông tin booking để tạo hợp đồng
    /// </summary>
    Task<BookingForContractViewModel?> GetBookingForContractAsync(Guid bookingId);
    
    /// <summary>
    /// Lấy options cho form tạo/sửa hợp đồng
    /// </summary>
    Task<ContractCreateOptionsViewModel> GetCreateOptionsAsync();
    
    /// <summary>
    /// Tạo hợp đồng mới
    /// </summary>
    Task<Guid> CreateContractAsync(ContractCreateViewModel model, Guid handledBy);
    
    /// <summary>
    /// Lấy thông tin edit hợp đồng
    /// </summary>
    Task<ContractEditViewModel?> GetContractForEditAsync(Guid contractId);
    
    /// <summary>
    /// Cập nhật hợp đồng
    /// </summary>
    Task<bool> UpdateContractAsync(ContractEditViewModel model, Guid updatedBy);
    
    // ===== Ký / Xác nhận =====
    
    /// <summary>
    /// Lấy thông tin ký hợp đồng (cho khách hàng)
    /// </summary>
    Task<ContractSignViewModel?> GetContractForSignAsync(Guid contractId);
    
    /// <summary>
    /// Khách hàng ký hợp đồng
    /// </summary>
    Task<bool> SignContractAsync(Guid contractId, Guid userId);
    
    /// <summary>
    /// Lấy thông tin xác nhận (cho nhân viên)
    /// </summary>
    Task<ContractConfirmViewModel?> GetContractForConfirmAsync(Guid contractId);
    
    /// <summary>
    /// Nhân viên xác nhận hợp đồng
    /// </summary>
    Task<bool> ConfirmContractAsync(Guid contractId, Guid confirmedBy, string? note = null);
    
    // ===== Hủy =====
    
    /// <summary>
    /// Lấy thông tin hủy hợp đồng
    /// </summary>
    Task<ContractCancelViewModel?> GetContractForCancelAsync(Guid contractId);
    
    /// <summary>
    /// Hủy hợp đồng
    /// </summary>
    Task<bool> CancelContractAsync(Guid contractId, Guid cancelledBy, string reason);
    
    // ===== 5.3 Gia hạn/Đổi xe =====
    
    /// <summary>
    /// Lấy thông tin gia hạn
    /// </summary>
    Task<ContractExtendViewModel?> GetContractForExtendAsync(Guid contractId);
    
    /// <summary>
    /// Gia hạn hợp đồng
    /// </summary>
    Task<bool> ExtendContractAsync(ContractExtendViewModel model, Guid extendedBy);
    
    // ===== Xuất/In =====
    
    /// <summary>
    /// Lấy thông tin in hợp đồng
    /// </summary>
    Task<ContractPrintViewModel?> GetContractForPrintAsync(Guid contractId);
    
    // ===== Helpers =====
    
    /// <summary>
    /// Kiểm tra xe có khả dụng trong khoảng thời gian không
    /// </summary>
    Task<bool> CheckVehicleAvailabilityAsync(Guid vehicleId, DateTime start, DateTime end, Guid? excludeContractId = null);
    
    /// <summary>
    /// Tính toán tiền thuê
    /// </summary>
    (int days, decimal rentalAmount, decimal total) CalculateRentalAmount(DateTime start, DateTime end, decimal unitPrice, decimal extraCharges);
    
    /// <summary>
    /// Sinh mã hợp đồng
    /// </summary>
    Task<string> GenerateContractCodeAsync();
    
    /// <summary>
    /// Kiểm tra khách hàng có đủ điều kiện thuê xe
    /// </summary>
    Task<(bool isEligible, string? reason)> CheckCustomerEligibilityAsync(Guid customerId);
}
