namespace UCar.Models.Enums
{
    /// <summary>
    /// Loại hóa đơn theo giai đoạn nghiệp vụ
    /// </summary>
    public enum InvoiceType
    {
        /// <summary>
        /// Hóa đơn đặt cọc - Khi ký hợp đồng
        /// Bao gồm: Cọc trách nhiệm + Cọc thuê xe
        /// </summary>
        Deposit = 1,

        /// <summary>
        /// Hóa đơn tiền thuê - Khi nhận xe (handover)
        /// Tổng tiền thuê - Cọc thuê xe đã trả
        /// </summary>
        Rental = 2,

        /// <summary>
        /// Hóa đơn phụ phí - Khi trả xe (return)
        /// Vượt giờ, vượt km, vệ sinh, nhiên liệu
        /// </summary>
        Surcharge = 3,

        /// <summary>
        /// Hóa đơn phạt - Khi trả xe (return)
        /// Vi phạm hợp đồng, hư hỏng xe
        /// </summary>
        Penalty = 4,

        /// <summary>
        /// Hóa đơn hoàn cọc - Sau 15-30 ngày từ khi trả xe
        /// Hoàn cọc trách nhiệm + Hoàn cọc thuê xe - Các khoản chưa thanh toán
        /// </summary>
        Refund = 5
    }
}
