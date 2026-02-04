namespace UCar.Models.Enums
{
    /// <summary>
    /// Trạng thái hóa đơn
    /// </summary>
    public enum InvoiceStatus
    {
        /// <summary>
        /// Nháp - Chưa phát hành
        /// </summary>
        Draft = 0,

        /// <summary>
        /// Đã phát hành - Chờ thanh toán
        /// </summary>
        Issued = 1,

        /// <summary>
        /// Đã thanh toán một phần
        /// </summary>
        PartiallyPaid = 2,

        /// <summary>
        /// Đã thanh toán đầy đủ
        /// </summary>
        Paid = 3,

        /// <summary>
        /// Quá hạn thanh toán
        /// </summary>
        Overdue = 4,

        /// <summary>
        /// Đã hủy
        /// </summary>
        Cancelled = 5,

        /// <summary>
        /// Đã hoàn tiền (cho Refund Invoice)
        /// </summary>
        Refunded = 6
    }
}
