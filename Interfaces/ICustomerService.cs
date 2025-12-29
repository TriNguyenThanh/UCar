using UCar.ViewModels;

namespace UCar.Interfaces;

/// <summary>
/// Customer service interface
/// Định nghĩa các operations cho quản lý khách hàng theo DFD 2.0
/// </summary>
public interface ICustomerService
{
    // DFD 2.2: Tra cứu lịch sử khách - List & Search
    Task<PaginatedList<CustomerListViewModel>> GetCustomersAsync(CustomerSearchViewModel searchModel);
    
    // DFD 2.2: Tra cứu lịch sử khách - Chi tiết khách hàng
    Task<CustomerDetailsViewModel?> GetCustomerDetailsAsync(Guid customerId);
    
    // DFD 2.1: Đăng ký thông tin khách - Tạo mới
    Task<(bool Success, string Message, Guid? CustomerId)> CreateCustomerAsync(CustomerCreateViewModel model);
    
    // Bổ sung hợp lý: Cập nhật thông tin khách hàng
    Task<(bool Success, string Message)> UpdateCustomerAsync(CustomerEditViewModel model);
    
    // Bổ sung hợp lý: Lấy thông tin để edit
    Task<CustomerEditViewModel?> GetCustomerForEditAsync(Guid customerId);
    
    // DFD 2.2: Kiểm tra tín nhiệm (Blacklist/Risk check)
    Task<(bool IsAllowed, string Message)> CheckCustomerEligibilityAsync(Guid customerId);
    
    // Bổ sung hợp lý: Khóa/Mở khóa khách hàng
    Task<(bool Success, string Message)> ToggleCustomerStatusAsync(Guid customerId, bool isActive);
    
    // Bổ sung hợp lý: Thêm vào blacklist
    Task<(bool Success, string Message)> ToggleBlacklistAsync(Guid customerId, bool isBlacklisted, string? reason);
    
    // Validation helpers
    Task<bool> IsUsernameExistsAsync(string username, Guid? excludeCustomerId = null);
    Task<bool> IsEmailExistsAsync(string email, Guid? excludeCustomerId = null);
    Task<bool> IsPhoneExistsAsync(string phone, Guid? excludeCustomerId = null);
    Task<bool> IsDocumentNumberExistsAsync(string documentNumber, Guid? excludeCustomerId = null);
}
