namespace UCar.Models.DTOs.Handover;

/// <summary>
/// DTO chi tiết biên bản giao xe
/// </summary>
public class HandoverRecordDetailDto
{
    public Guid HandoverId { get; set; }
    public Guid ContractId { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    
    // Customer
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerIdNumber { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    
    // Vehicle
    public string PlateNo { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;
    public string VehicleTypeName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    
    // Handover details
    public decimal OdoKmOut { get; set; }
    public decimal FuelLevelOut { get; set; }
    public DateTime HandedAt { get; set; }
    public string? ExteriorCondition { get; set; }
    public string? InteriorCondition { get; set; }
    public string? PreExistingDamages { get; set; }
    public List<string> VehicleImages { get; set; } = new();
    
    // Accessories
    public List<AccessoryDto> Accessories { get; set; } = new();
    
    // Confirmation
    public bool CustomerConfirmed { get; set; }
    public DateTime? CustomerConfirmedAt { get; set; }
    public string? Note { get; set; }
    
    // Staff
    public string HandedOverByName { get; set; } = string.Empty;
    
    // Branch
    public string BranchName { get; set; } = string.Empty;
    public string BranchAddress { get; set; } = string.Empty;
    public string BranchPhone { get; set; } = string.Empty;
    
    // Contract info for print
    public DateTime PlannedStart { get; set; }
    public DateTime PlannedEnd { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal RentalAmount { get; set; }
}

/// <summary>
/// DTO chi tiết biên bản nhận xe
/// </summary>
public class ReturnRecordDetailDto
{
    public Guid ReturnId { get; set; }
    public Guid ContractId { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    
    // Customer
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    
    // Vehicle
    public string PlateNo { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;
    
    // Comparison (Out vs In)
    public decimal OdoKmOut { get; set; }
    public decimal OdoKmIn { get; set; }
    public decimal KmDriven => OdoKmIn - OdoKmOut;
    
    public decimal FuelLevelOut { get; set; }
    public decimal FuelLevelIn { get; set; }
    public decimal FuelShortage { get; set; }
    
    // Return details
    public DateTime HandedAt { get; set; }
    public DateTime ReturnedAt { get; set; }
    public decimal OvertimeHours { get; set; }
    
    public string? ExteriorCondition { get; set; }
    public string? InteriorCondition { get; set; }
    public string? DamagesFound { get; set; }
    public List<string> VehicleImages { get; set; } = new();
    
    public bool NeedsCleaning { get; set; }
    public bool NeedsMaintenance { get; set; }
    
    // Accessories
    public List<AccessoryReturnDto> AccessoriesReturned { get; set; } = new();
    
    // Charges
    public List<ChargeDto> AdditionalCharges { get; set; } = new();
    public decimal TotalCharges => AdditionalCharges.Sum(c => c.Amount);
    
    // Rental cost info (ĐÃ THANH TOÁN KHI GIAO XE - chỉ để hiển thị tham khảo)
    public int RentalDays { get; set; }
    public decimal RentalUnitPrice { get; set; }
    public decimal RentalAmount { get; set; } // Tiền thuê - ĐÃ THANH TOÁN
    
    // Deposit info
    public decimal DepositAmount { get; set; } // Tổng cọc = Cọc TN + Cọc thuê
    public decimal ResponsibilityDeposit { get; set; } // Cọc trách nhiệm
    public decimal RentalDeposit { get; set; } // Cọc thuê xe
    
    // Accessory damage
    public decimal AccessoryDamageCost => AccessoriesReturned.Where(a => !a.IsReturnedOk).Sum(a => a.EstimatedValue);
    
    // === LOGIC MỚI: Tính tiền khi trả xe ===
    // Tổng phí phát sinh = Phụ phí + Chi phí phụ kiện hư hỏng
    public decimal TotalSurcharges => TotalCharges + AccessoryDamageCost;
    
    // Số tiền cần hoàn = Cọc - Phí phát sinh
    // Dương: Hoàn tiền cho khách
    // Âm: Khách phải trả thêm
    public decimal RefundAmount => DepositAmount - TotalSurcharges;
    
    // AmountDue: Số tiền khách phải trả (dương = khách trả, âm = hoàn khách)
    public decimal AmountDue => -RefundAmount; // Đảo dấu: dương = khách trả thêm, âm = hoàn khách
    
    // GrandTotal giữ lại để tương thích (nhưng không dùng để tính tiền)
    public decimal GrandTotal => TotalSurcharges; // Chỉ tính phí phát sinh, KHÔNG tính tiền thuê
    
    // Payment status
    public bool IsPaid { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount => AmountDue - AmountPaid;
    
    public string? Note { get; set; }
    
    // Staff
    public string ReceivedByName { get; set; } = string.Empty;
    
    // Branch
    public string BranchName { get; set; } = string.Empty;
}

/// <summary>
/// DTO danh sách biên bản giao nhận
/// </summary>
public class HandoverDocumentListDto
{
    public Guid DocumentId { get; set; }
    public string DocumentType { get; set; } = string.Empty; // "Handover" / "Return"
    public Guid ContractId { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string PlateNo { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
}

/// <summary>
/// DTO filter danh sách biên bản
/// </summary>
public class HandoverDocumentFilterDto
{
    public string? SearchTerm { get; set; }
    public string? DocumentType { get; set; } // "Handover" / "Return" / null = all
    public Guid? BranchId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
