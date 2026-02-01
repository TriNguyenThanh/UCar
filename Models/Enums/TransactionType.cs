namespace UCar.Models.Enums;

public enum TransactionType
{
    ResponsibilityDeposit,  // Cọc trách nhiệm (cố định 2M)
    RentalDeposit,          // Cọc thuê xe (50% giá trị hợp đồng)
    RentalFee,              // Tiền thuê xe
    Penalty,                // Phạt vi phạm
    RefundResponsibility,   // Hoàn cọc trách nhiệm
    RefundRental            // Hoàn cọc thuê xe
}
