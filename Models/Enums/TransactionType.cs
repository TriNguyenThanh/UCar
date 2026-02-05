namespace UCar.Models.Enums;

public enum TransactionType
{
    // ===== GIAO XE (Pickup) =====
    /// <summary>Thanh toán khi giao xe (tiền thuê + cọc + phụ kiện)</summary>
    PickupPayment,
    
    // ===== TRẢ XE (Return) =====
    /// <summary>Thanh toán phí phát sinh khi trả xe (nếu khách phải bù thêm)</summary>
    ReturnPayment,
    
    /// <summary>Hoàn tiền cho khách khi trả xe (nếu không có phí phát sinh)</summary>
    ReturnRefund,
    
    // ===== PHÍ PHẠT =====
    /// <summary>Phí phạt vi phạm hợp đồng</summary>
    Penalty,
    
    // ===== LEGACY (để tương thích ngược) =====
    ResponsibilityDeposit,  // Cọc trách nhiệm (cố định 2M)
    RentalDeposit,          // Cọc thuê xe (50% giá trị hợp đồng)
    RentalFee,              // Tiền thuê xe
    RefundResponsibility,   // Hoàn cọc trách nhiệm
    RefundRental,           // Hoàn cọc thuê xe
    Deposit,                // Thanh toán Deposit Invoice
    Surcharge,              // Thanh toán Surcharge Invoice
    PenaltyFee              // Thanh toán Penalty Invoice
}
