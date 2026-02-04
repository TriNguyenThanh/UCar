namespace UCar.Models.Enums
{
    /// <summary>
    /// Loại điều chỉnh hóa đơn
    /// </summary>
    public enum AdjustmentType
    {
        /// <summary>
        /// Sửa lỗi
        /// </summary>
        Correction = 1,

        /// <summary>
        /// Giảm giá
        /// </summary>
        Discount = 2,

        /// <summary>
        /// Điều chỉnh thuế
        /// </summary>
        TaxAdjustment = 3,

        /// <summary>
        /// Khác
        /// </summary>
        Other = 99
    }
}
