namespace UCar.Models.Enums
{
    /// <summary>
    /// Loại dòng chi tiết trong hóa đơn
    /// </summary>
    public enum LineItemType
    {
        // ===== DEPOSIT INVOICE =====
        /// <summary>
        /// Cọc trách nhiệm (từ DepositPolicy.ResponsibilityDepositAmount)
        /// </summary>
        ResponsibilityDeposit = 1,

        /// <summary>
        /// Cọc thuê xe (từ DepositPolicy - tính theo % hoặc cố định)
        /// </summary>
        RentalDeposit = 2,

        // ===== RENTAL INVOICE =====
        /// <summary>
        /// Tiền thuê ngày thường (từ ContractPriceBreakdown - PriceType.Base)
        /// </summary>
        BaseRental = 3,

        /// <summary>
        /// Tiền thuê ngày lễ (từ ContractPriceBreakdown - PriceType.Peak)
        /// </summary>
        PeakRental = 4,

        /// <summary>
        /// Tiền thuê theo tháng (từ ContractPriceBreakdown - PriceType.Monthly)
        /// </summary>
        MonthlyRental = 5,

        // ===== SURCHARGE INVOICE =====
        /// <summary>
        /// Phụ phí vượt giờ (từ Contract.SnapshotOvertimeHourlyPrice)
        /// </summary>
        OvertimeSurcharge = 6,

        /// <summary>
        /// Phụ phí vượt km (từ SurchargePolicy - ExtraKilometer)
        /// </summary>
        ExtraKmSurcharge = 7,

        /// <summary>
        /// Phí vệ sinh (từ SurchargePolicy - CleaningFee)
        /// </summary>
        CleaningSurcharge = 8,

        /// <summary>
        /// Phí thiếu nhiên liệu (từ SurchargePolicy - FuelShortage)
        /// </summary>
        FuelSurcharge = 9,

        // ===== PENALTY INVOICE =====
        /// <summary>
        /// Phạt vi phạm hợp đồng (từ ContractViolation → ContractCharge)
        /// </summary>
        ContractViolation = 10,

        /// <summary>
        /// Phạt hư hỏng xe (từ ContractCharge)
        /// </summary>
        DamagePenalty = 11,

        // ===== REFUND INVOICE =====
        /// <summary>
        /// Hoàn cọc trách nhiệm (negative amount)
        /// </summary>
        RefundResponsibility = 12,

        /// <summary>
        /// Hoàn cọc thuê xe (negative amount)
        /// </summary>
        RefundRental = 13,

        // ===== ADJUSTMENTS =====
        /// <summary>
        /// Giảm giá
        /// </summary>
        Discount = 14,

        /// <summary>
        /// Thuế VAT
        /// </summary>
        Tax = 15,

        // ===== DELIVERY INVOICE (LUỒNG MỚI) =====
        /// <summary>
        /// Phụ kiện thuê kèm (GPS, ghế trẻ em, v.v.)
        /// </summary>
        Accessory = 16,

        /// <summary>
        /// Phí bảo hiểm
        /// </summary>
        Insurance = 17,

        /// <summary>
        /// Khác
        /// </summary>
        Other = 99
    }
}
