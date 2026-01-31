namespace UCar.Models.Enums;

public enum TransactionType
{
    /// <summary>Đặt cọc ban đầu</summary>
    Deposit,
    
    /// <summary>Thanh toán tiền thuê xe</summary>
    RentalFee,
    
    /// <summary>Phụ phí (overtime, extra km, cleaning, etc.)</summary>
    Surcharge,
    
    /// <summary>Phạt vi phạm hợp đồng</summary>
    Penalty,
    
    /// <summary>Hoàn lại tiền cọc</summary>
    RefundDeposit,
    
    /// <summary>Hoàn tiền (hủy booking, overcharge, etc.)</summary>
    Refund
}
