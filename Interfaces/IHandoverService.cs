using UCar.Models.DTOs;
using UCar.Models.DTOs.Handover;
using UCar.Models.DTOs.Vehicle;


namespace UCar.Interfaces;

/// <summary>
/// Interface quản lý giao nhận xe
/// Tương ứng DFD 6.1-6.4
/// </summary>
public interface IHandoverService
{
    #region Contract List for Handover

    /// <summary>
    /// Lấy danh sách hợp đồng chờ giao xe
    /// </summary>
    Task<PagedResult<ContractForHandoverListDto>> GetContractsForHandoverAsync(HandoverFilterDto filter);

    /// <summary>
    /// Lấy danh sách hợp đồng chờ nhận xe
    /// </summary>
    Task<PagedResult<ContractForHandoverListDto>> GetContractsForReturnAsync(HandoverFilterDto filter);

    /// <summary>
    /// Lấy danh sách hợp đồng quá hạn
    /// </summary>
    Task<IEnumerable<ContractForHandoverListDto>> GetOverdueContractsAsync();

    #endregion

    #region Check-Out (Handover)

    /// <summary>
    /// Lấy form check-out cho hợp đồng
    /// </summary>
    Task<CheckOutFormDto?> GetCheckOutFormAsync(Guid contractId);

    /// <summary>
    /// Kiểm tra có thể giao xe không (đã đặt cọc, xe sẵn sàng...)
    /// </summary>
    Task<ServiceResult> CanCheckOutAsync(Guid contractId);

    /// <summary>
    /// Xác nhận giao xe cho khách (Check-out) - Luồng mới
    /// - Tạo HandoverRecord
    /// - Cập nhật trạng thái hợp đồng → PendingSigning (chờ ký + thanh toán)
    /// </summary>
    Task<ServiceResult<Guid>> ConfirmCheckOutAsync(CheckOutDto dto, Guid userId);

    /// <summary>
    /// Hoàn tất giao xe - Luồng mới
    /// Gọi sau khi khách đã ký hợp đồng và thanh toán
    /// - Cập nhật trạng thái xe → Renting
    /// - Cập nhật trạng thái hợp đồng → Active
    /// </summary>
    Task<ServiceResult> CompleteHandoverAsync(Guid contractId, Guid staffId, Guid? paymentTxnId = null);

    #endregion

    #region Check-In (Return)

    /// <summary>
    /// Lấy form check-in cho hợp đồng
    /// </summary>
    Task<CheckInFormDto?> GetCheckInFormAsync(Guid contractId);

    /// <summary>
    /// Kiểm tra có thể nhận xe không (hợp đồng đang thuê...)
    /// </summary>
    Task<ServiceResult> CanCheckInAsync(Guid contractId);

    /// <summary>
    /// Xác nhận nhận xe từ khách (Check-in)
    /// - Tạo ReturnRecord
    /// - Tính toán phụ phí (trễ hạn, nhiên liệu, hư hỏng...)
    /// - Cập nhật trạng thái xe → Available/Maintenance
    /// - Cập nhật trạng thái hợp đồng → PendingSettlement/Completed
    /// </summary>
    Task<ServiceResult<Guid>> ConfirmCheckInAsync(CheckInDto dto, Guid userId);

    /// <summary>
    /// Hoàn tất trả xe - Luồng mới
    /// Gọi sau khi khách thanh toán/hoàn tiền xong
    /// - Tạo Return Invoice tổng hợp
    /// - Cập nhật trạng thái hợp đồng → Completed
    /// </summary>
    Task<ServiceResult> CompleteReturnAsync(Guid contractId, Guid staffId, Guid? paymentTxnId = null);

    #endregion

    #region Documents

    /// <summary>
    /// Lấy chi tiết biên bản giao xe
    /// </summary>
    Task<HandoverRecordDetailDto?> GetHandoverRecordAsync(Guid contractId);

    /// <summary>
    /// Lấy chi tiết biên bản nhận xe
    /// </summary>
    Task<ReturnRecordDetailDto?> GetReturnRecordAsync(Guid contractId);

    /// <summary>
    /// Lấy danh sách biên bản giao/nhận
    /// </summary>
    Task<PagedResult<HandoverDocumentListDto>> GetDocumentsAsync(HandoverDocumentFilterDto filter);

    #endregion

    #region Incidents (D13)

    /// <summary>
    /// Lấy danh sách sự cố của hợp đồng
    /// </summary>
    Task<IEnumerable<IncidentDto>> GetAllIncidentsAsync();
    Task<IEnumerable<IncidentDto>> GetIncidentsByContractAsync(Guid contractId);

    /// <summary>
    /// Tạo sự cố mới
    /// </summary>
    Task<ServiceResult<Guid>> CreateIncidentAsync(IncidentCreateDto dto, Guid userId);

    #endregion

    #region Violations (D13)

    /// <summary>
    /// Lấy danh sách vi phạm của hợp đồng
    /// </summary>
    Task<IEnumerable<ViolationDto>> GetViolationsByContractAsync(Guid contractId);

    /// <summary>
    /// Tạo vi phạm mới
    /// </summary>
    Task<ServiceResult<Guid>> CreateViolationAsync(ViolationCreateDto dto, Guid userId);

    #endregion
}
