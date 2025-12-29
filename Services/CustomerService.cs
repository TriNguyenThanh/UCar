using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models;
using UCar.Models.Enums;
using UCar.ViewModels;

namespace UCar.Services;

/// <summary>
/// Customer service implementation
/// Implements business logic for customer management according to DFD 2.0
/// </summary>
public class CustomerService : ICustomerService
{
    private readonly UCarDbContext _context;
    private readonly ILogger<CustomerService> _logger;
    private readonly IWebHostEnvironment _environment;

    public CustomerService(
        UCarDbContext context, 
        ILogger<CustomerService> logger,
        IWebHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _environment = environment;
    }

    #region DFD 2.2: Tra cứu lịch sử khách - List & Search

    public async Task<PaginatedList<CustomerListViewModel>> GetCustomersAsync(CustomerSearchViewModel searchModel)
    {
        try
        {
            _logger.LogInformation("Fetching customers with search criteria: {SearchTerm}", searchModel.SearchTerm);

            var query = _context.Customers
                .Include(c => c.UserAccount)
                .Include(c => c.Documents)
                .AsQueryable();

            // Search filter (tìm kiếm theo tên, email, SĐT, CCCD)
            if (!string.IsNullOrWhiteSpace(searchModel.SearchTerm))
            {
                var searchTerm = searchModel.SearchTerm.ToLower().Trim();
                query = query.Where(c =>
                    c.FullName.ToLower().Contains(searchTerm) ||
                    (c.UserAccount.Email != null && c.UserAccount.Email.ToLower().Contains(searchTerm)) ||
                    (c.UserAccount.Phone != null && c.UserAccount.Phone.Contains(searchTerm)) ||
                    c.Documents.Any(d => d.DocNumber != null && d.DocNumber.Contains(searchTerm))
                );
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(searchModel.StatusFilter))
            {
                switch (searchModel.StatusFilter.ToLower())
                {
                    case "active":
                        query = query.Where(c => c.UserAccount.IsActive && !c.IsBlacklisted);
                        break;
                    case "inactive":
                        query = query.Where(c => !c.UserAccount.IsActive);
                        break;
                    case "blacklisted":
                        query = query.Where(c => c.IsBlacklisted);
                        break;
                }
            }

            // Risk level filter
            if (!string.IsNullOrWhiteSpace(searchModel.RiskLevelFilter))
            {
                query = query.Where(c => c.RiskLevel == searchModel.RiskLevelFilter);
            }

            // Sorting
            query = searchModel.SortBy?.ToLower() switch
            {
                "name" => query.OrderBy(c => c.FullName),
                "name_desc" => query.OrderByDescending(c => c.FullName),
                "date" => query.OrderBy(c => c.CreatedAt),
                "date_desc" => query.OrderByDescending(c => c.CreatedAt),
                _ => query.OrderByDescending(c => c.CreatedAt) // Default: newest first
            };

            // Project to ViewModel
            var vmQuery = query.Select(c => new CustomerListViewModel
            {
                CustomerId = c.CustomerId,
                FullName = c.FullName,
                Email = c.UserAccount.Email,
                Phone = c.UserAccount.Phone,
                IdCardNumber = c.Documents
                    .Where(d => d.DocType == CustomerDocumentType.IdCard)
                    .Select(d => d.DocNumber)
                    .FirstOrDefault(),
                AddressText = c.AddressText,
                RiskLevel = c.RiskLevel,
                IsBlacklisted = c.IsBlacklisted,
                IsActive = c.UserAccount.IsActive,
                CreatedAt = c.CreatedAt,
                TotalBookings = c.Bookings.Count
            });

            return await PaginatedList<CustomerListViewModel>.CreateAsync(
                vmQuery, 
                searchModel.PageNumber, 
                searchModel.PageSize
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customers");
            throw;
        }
    }

    #endregion

    #region DFD 2.2: Tra cứu lịch sử khách - Chi tiết

    public async Task<CustomerDetailsViewModel?> GetCustomerDetailsAsync(Guid customerId)
    {
        try
        {
            _logger.LogInformation("Fetching customer details for ID: {CustomerId}", customerId);

            var customer = await _context.Customers
                .Include(c => c.UserAccount)
                .Include(c => c.Documents)
                .Include(c => c.Bookings)
                    .ThenInclude(b => b.VehicleType)
                .Include(c => c.Bookings)
                    .ThenInclude(b => b.AssignedVehicle)
                        .ThenInclude(v => v!.Model)
                .Include(c => c.RentalContracts)
                    .ThenInclude(rc => rc.Violations)
                .Include(c => c.PaymentTransactions)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer == null)
            {
                _logger.LogWarning("Customer not found: {CustomerId}", customerId);
                return null;
            }

            // Map to ViewModel
            var viewModel = new CustomerDetailsViewModel
            {
                CustomerId = customer.CustomerId,
                FullName = customer.FullName,
                Email = customer.UserAccount.Email,
                Phone = customer.UserAccount.Phone,
                Username = customer.UserAccount.Username,
                Dob = customer.Dob,
                AddressText = customer.AddressText,
                RiskLevel = customer.RiskLevel,
                IsBlacklisted = customer.IsBlacklisted,
                IsActive = customer.UserAccount.IsActive,
                CreatedAt = customer.CreatedAt,
                LastLoginAt = customer.UserAccount.LastLoginAt,
                Documents = customer.Documents.Select(d => new CustomerDocumentViewModel
                {
                    DocId = d.DocId,
                    DocType = d.DocType,
                    DocNumber = d.DocNumber,
                    IssuedDate = d.IssuedDate,
                    IssuedPlace = d.IssuedPlace,
                    IsVerified = d.IsVerified,
                    ImageFrontUrl = d.ImageFrontUrl,
                    ImageBackUrl = d.ImageBackUrl
                }).ToList(),
                RentalHistory = new CustomerRentalHistoryViewModel
                {
                    TotalBookings = customer.Bookings.Count,
                    CompletedBookings = customer.Bookings.Count(b => b.Status == BookingStatus.Completed),
                    CancelledBookings = customer.Bookings.Count(b => b.Status == BookingStatus.Cancelled),
                    TotalContracts = customer.RentalContracts.Count,
                    ActiveContracts = customer.RentalContracts.Count(rc => rc.Status == RentalContractStatus.Active),
                    TotalViolations = customer.RentalContracts.SelectMany(rc => rc.Violations).Count(),
                    PendingViolations = customer.RentalContracts
                        .SelectMany(rc => rc.Violations)
                        .Count(v => v.Status == ViolationStatus.Open),
                    TotalPaid = customer.PaymentTransactions
                        .Where(pt => pt.Status == TransactionStatus.Success)
                        .Sum(pt => pt.Amount),
                    CurrentDebt = customer.PaymentTransactions
                        .Where(pt => pt.Status == TransactionStatus.Failed)
                        .Sum(pt => pt.Amount),
                    RecentBookings = customer.Bookings
                        .OrderByDescending(b => b.CreatedAt)
                        .Take(5)
                        .Select(b => new RecentBookingViewModel
                        {
                            BookingId = b.BookingId,
                            VehicleName = b.AssignedVehicle != null ? 
                                $"{b.AssignedVehicle.Model.ModelName}" : 
                                $"{b.VehicleType.TypeName}",
                            StartDate = b.StartAt,
                            EndDate = b.EndAt,
                            Status = b.Status.ToString(),
                            TotalAmount = b.EstimatedTotal
                        }).ToList()
                }
            };

            return viewModel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customer details for ID: {CustomerId}", customerId);
            throw;
        }
    }

    #endregion

    #region DFD 2.1: Đăng ký thông tin khách - Create

    public async Task<(bool Success, string Message, Guid? CustomerId)> CreateCustomerAsync(CustomerCreateViewModel model)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _logger.LogInformation("Creating new customer: {Username}", model.Username);

            // Validate unique constraints
            if (await IsUsernameExistsAsync(model.Username))
            {
                return (false, "Tên đăng nhập đã tồn tại", null);
            }

            if (await IsEmailExistsAsync(model.Email))
            {
                return (false, "Email đã được sử dụng", null);
            }

            if (await IsPhoneExistsAsync(model.Phone))
            {
                return (false, "Số điện thoại đã được sử dụng", null);
            }

            if (await IsDocumentNumberExistsAsync(model.DocumentNumber))
            {
                return (false, $"Số {model.DocumentType} đã được đăng ký", null);
            }

            // Get or create Customer role
            var customerRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Code == RoleCode.Customer);

            if (customerRole == null)
            {
                _logger.LogError("Customer role not found in database");
                return (false, "Lỗi hệ thống: không tìm thấy vai trò khách hàng", null);
            }

            // Create UserAccount
            var userAccount = new UserAccount
            {
                UserId = Guid.NewGuid(),
                RoleId = customerRole.RoleId,
                Username = model.Username,
                Email = model.Email,
                Phone = model.Phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password), // Use BCrypt for password hashing
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.UserAccounts.AddAsync(userAccount);

            // Create Customer
            var customer = new Customer
            {
                CustomerId = Guid.NewGuid(),
                UserId = userAccount.UserId,
                FullName = model.FullName,
                Dob = model.Dob,
                AddressText = model.AddressText,
                RiskLevel = "Low", // Default risk level
                IsBlacklisted = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Customers.AddAsync(customer);

            // Save document images
            string? frontImagePath = null;
            string? backImagePath = null;

            if (model.DocumentImageFront != null)
            {
                frontImagePath = await SaveDocumentImageAsync(model.DocumentImageFront, customer.CustomerId, "front");
            }

            if (model.DocumentImageBack != null)
            {
                backImagePath = await SaveDocumentImageAsync(model.DocumentImageBack, customer.CustomerId, "back");
            }

            // Create CustomerDocument (CCCD/GPLX - từ DFD 2.1)
            var document = new CustomerDocument
            {
                DocId = Guid.NewGuid(),
                CustomerId = customer.CustomerId,
                DocType = model.DocumentType,
                DocNumber = model.DocumentNumber,
                IssuedDate = model.DocumentIssuedDate,
                IssuedPlace = model.DocumentIssuedPlace,
                ImageFrontUrl = frontImagePath,
                ImageBackUrl = backImagePath,
                IsVerified = false // Needs manual verification
            };

            await _context.CustomerDocuments.AddAsync(document);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Customer created successfully: {CustomerId}", customer.CustomerId);
            return (true, "Tạo khách hàng thành công", customer.CustomerId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating customer: {Username}", model.Username);
            return (false, $"Lỗi tạo khách hàng: {ex.Message}", null);
        }
    }

    #endregion

    #region Update Customer

    public async Task<CustomerEditViewModel?> GetCustomerForEditAsync(Guid customerId)
    {
        try
        {
            var customer = await _context.Customers
                .Include(c => c.UserAccount)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer == null)
            {
                return null;
            }

            return new CustomerEditViewModel
            {
                CustomerId = customer.CustomerId,
                FullName = customer.FullName,
                Email = customer.UserAccount.Email ?? string.Empty,
                Phone = customer.UserAccount.Phone ?? string.Empty,
                Dob = customer.Dob,
                AddressText = customer.AddressText,
                RiskLevel = customer.RiskLevel,
                IsBlacklisted = customer.IsBlacklisted,
                IsActive = customer.UserAccount.IsActive,
                CreatedAt = customer.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customer for edit: {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<(bool Success, string Message)> UpdateCustomerAsync(CustomerEditViewModel model)
    {
        try
        {
            _logger.LogInformation("Updating customer: {CustomerId}", model.CustomerId);

            var customer = await _context.Customers
                .Include(c => c.UserAccount)
                .FirstOrDefaultAsync(c => c.CustomerId == model.CustomerId);

            if (customer == null)
            {
                return (false, "Không tìm thấy khách hàng");
            }

            // Validate unique constraints (excluding current customer)
            if (await IsEmailExistsAsync(model.Email, model.CustomerId))
            {
                return (false, "Email đã được sử dụng bởi khách hàng khác");
            }

            if (await IsPhoneExistsAsync(model.Phone, model.CustomerId))
            {
                return (false, "Số điện thoại đã được sử dụng bởi khách hàng khác");
            }

            // Update Customer
            customer.FullName = model.FullName;
            customer.Dob = model.Dob;
            customer.AddressText = model.AddressText;
            customer.RiskLevel = model.RiskLevel;
            customer.IsBlacklisted = model.IsBlacklisted;

            // Update UserAccount
            customer.UserAccount.Email = model.Email;
            customer.UserAccount.Phone = model.Phone;
            customer.UserAccount.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Customer updated successfully: {CustomerId}", model.CustomerId);
            return (true, "Cập nhật khách hàng thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer: {CustomerId}", model.CustomerId);
            return (false, $"Lỗi cập nhật khách hàng: {ex.Message}");
        }
    }

    #endregion

    #region DFD 2.2: Kiểm tra tín nhiệm (Check Eligibility)

    public async Task<(bool IsAllowed, string Message)> CheckCustomerEligibilityAsync(Guid customerId)
    {
        try
        {
            var customer = await _context.Customers
                .Include(c => c.UserAccount)
                .Include(c => c.RentalContracts)
                .Include(c => c.PaymentTransactions)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer == null)
            {
                return (false, "Khách hàng không tồn tại");
            }

            // Check if blacklisted
            if (customer.IsBlacklisted)
            {
                return (false, "Khách hàng nằm trong danh sách đen");
            }

            // Check if account is active
            if (!customer.UserAccount.IsActive)
            {
                return (false, "Tài khoản khách hàng đã bị khóa");
            }

            // Check for failed payments/debts
            var pendingDebt = await _context.PaymentTransactions
                .Where(pt => pt.CustomerId == customerId && pt.Status == TransactionStatus.Failed)
                .SumAsync(pt => pt.Amount);

            if (pendingDebt > 0)
            {
                return (false, $"Khách hàng có công nợ chưa thanh toán: {pendingDebt:N0}đ");
            }

            // Check for open violations
            var pendingViolations = await _context.ContractViolations
                .Include(cv => cv.RentalContract)
                .Where(cv => cv.RentalContract.CustomerId == customerId && cv.Status == ViolationStatus.Open)
                .CountAsync();

            if (pendingViolations > 0)
            {
                return (false, $"Khách hàng có {pendingViolations} vi phạm chưa xử lý");
            }

            return (true, "Khách hàng đủ điều kiện thuê xe");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking customer eligibility: {CustomerId}", customerId);
            throw;
        }
    }

    #endregion

    #region Toggle Status & Blacklist

    public async Task<(bool Success, string Message)> ToggleCustomerStatusAsync(Guid customerId, bool isActive)
    {
        try
        {
            var customer = await _context.Customers
                .Include(c => c.UserAccount)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer == null)
            {
                return (false, "Không tìm thấy khách hàng");
            }

            // Check if customer has active contracts
            if (!isActive)
            {
                var hasActiveContracts = await _context.RentalContracts
                    .AnyAsync(rc => rc.CustomerId == customerId && rc.Status == RentalContractStatus.Active);

                if (hasActiveContracts)
                {
                    return (false, "Không thể khóa tài khoản: Khách hàng đang có hợp đồng đang hoạt động");
                }
            }

            customer.UserAccount.IsActive = isActive;
            await _context.SaveChangesAsync();

            var action = isActive ? "mở khóa" : "khóa";
            _logger.LogInformation("Customer {CustomerId} {Action}", customerId, action);
            return (true, $"Đã {action} tài khoản thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling customer status: {CustomerId}", customerId);
            return (false, $"Lỗi: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> ToggleBlacklistAsync(Guid customerId, bool isBlacklisted, string? reason)
    {
        try
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer == null)
            {
                return (false, "Không tìm thấy khách hàng");
            }

            customer.IsBlacklisted = isBlacklisted;
            await _context.SaveChangesAsync();

            var action = isBlacklisted ? "thêm vào" : "xóa khỏi";
            _logger.LogInformation("Customer {CustomerId} {Action} blacklist. Reason: {Reason}", customerId, action, reason);
            return (true, $"Đã {action} danh sách đen");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling blacklist: {CustomerId}", customerId);
            return (false, $"Lỗi: {ex.Message}");
        }
    }

    #endregion

    #region Validation Helpers

    public async Task<bool> IsUsernameExistsAsync(string username, Guid? excludeCustomerId = null)
    {
        var query = _context.UserAccounts.Where(u => u.Username == username);
        
        if (excludeCustomerId.HasValue)
        {
            query = query.Where(u => u.UserId != excludeCustomerId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<bool> IsEmailExistsAsync(string email, Guid? excludeCustomerId = null)
    {
        var query = _context.UserAccounts.Where(u => u.Email == email);
        
        if (excludeCustomerId.HasValue)
        {
            // Get UserAccount.UserId for the customer
            var userIds = await _context.Customers
                .Where(c => c.CustomerId != excludeCustomerId.Value)
                .Select(c => c.UserId)
                .ToListAsync();
            
            query = query.Where(u => userIds.Contains(u.UserId));
        }

        return await query.AnyAsync();
    }

    public async Task<bool> IsPhoneExistsAsync(string phone, Guid? excludeCustomerId = null)
    {
        var query = _context.UserAccounts.Where(u => u.Phone == phone);
        
        if (excludeCustomerId.HasValue)
        {
            var userIds = await _context.Customers
                .Where(c => c.CustomerId != excludeCustomerId.Value)
                .Select(c => c.UserId)
                .ToListAsync();
            
            query = query.Where(u => userIds.Contains(u.UserId));
        }

        return await query.AnyAsync();
    }

    public async Task<bool> IsDocumentNumberExistsAsync(string documentNumber, Guid? excludeCustomerId = null)
    {
        var query = _context.CustomerDocuments.Where(d => d.DocNumber == documentNumber);
        
        if (excludeCustomerId.HasValue)
        {
            query = query.Where(d => d.CustomerId != excludeCustomerId.Value);
        }

        return await query.AnyAsync();
    }

    #endregion

    #region Private Helper Methods

    private async Task<string> SaveDocumentImageAsync(IFormFile file, Guid customerId, string side)
    {
        try
        {
            // Create uploads directory if not exists
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "customers", customerId.ToString());
            Directory.CreateDirectory(uploadsFolder);

            // Generate unique filename
            var fileName = $"{side}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative path for database
            return $"/uploads/customers/{customerId}/{fileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving document image");
            throw;
        }
    }

    #endregion
}
