namespace UCar.Models.Enums
{
    /// <summary>
    /// Loại hóa đơn theo giai đoạn nghiệp vụ
    /// </summary>
    public enum InvoiceType
    {
        /// <summary>
        /// Hóa đơn đặt cọc - Khi ký hợp đồng (LEGACY)
        /// Bao gồm: Cọc trách nhiệm + Cọc thuê xe
        /// </summary>
        Deposit = 1,

        /// <summary>
        /// Hóa đơn tiền thuê - Khi nhận xe (handover) (LEGACY)
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
        Refund = 5,

        // ===== LUỒNG MỚI =====
        
        /// <summary>
        /// HÓA ĐƠN GIAO XE (Delivery Invoice) - Thanh toán tại quầy khi ký HĐ
        /// = Tiền cọc trách nhiệm + Tiền thuê + Cọc thuê + Phụ kiện
        /// </summary>
        Delivery = 6,

        /// <summary>
        /// HÓA ĐƠN TRẢ XE (Return Invoice) - Thanh toán/hoàn tiền khi trả xe
        /// = Tiền hoàn lại (nếu không phí phát sinh) hoặc Tiền khách bù thêm (nếu có phí)
        /// Công thức: Cọc đã đặt - Phí phát sinh
        /// </summary>
        Return = 7
    }
}
