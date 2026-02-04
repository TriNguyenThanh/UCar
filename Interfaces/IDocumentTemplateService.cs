using UCar.ViewModels.Contract;

namespace UCar.Interfaces;

/// <summary>
/// Service xử lý template Word/DOCX cho Hợp đồng và Hóa đơn
/// </summary>
public interface IDocumentTemplateService
{
    /// <summary>
    /// Tạo file PDF Hợp đồng từ template DOCX
    /// </summary>
    /// <param name="contractId">ID hợp đồng</param>
    /// <returns>Byte array của file PDF</returns>
    Task<byte[]> GenerateContractPdfAsync(Guid contractId);

    /// <summary>
    /// Tạo file DOCX Hợp đồng từ template
    /// </summary>
    /// <param name="contractId">ID hợp đồng</param>
    /// <returns>Byte array của file DOCX</returns>
    Task<byte[]> GenerateContractDocxAsync(Guid contractId);

    /// <summary>
    /// Tạo file PDF Biên bản giao xe từ template DOCX
    /// </summary>
    /// <param name="handoverRecordId">ID biên bản giao xe</param>
    /// <returns>Byte array của file PDF</returns>
    Task<byte[]> GenerateHandoverPdfAsync(Guid handoverRecordId);

    /// <summary>
    /// Tạo file DOCX Biên bản giao xe từ template
    /// </summary>
    /// <param name="handoverRecordId">ID biên bản giao xe</param>
    /// <returns>Byte array của file DOCX</returns>
    Task<byte[]> GenerateHandoverDocxAsync(Guid handoverRecordId);

    /// <summary>
    /// Tạo file PDF Hóa đơn từ template DOCX
    /// </summary>
    /// <param name="invoiceId">ID hóa đơn</param>
    /// <returns>Byte array của file PDF</returns>
    Task<byte[]> GenerateInvoicePdfAsync(Guid invoiceId);

    /// <summary>
    /// Tạo file DOCX Hóa đơn từ template
    /// </summary>
    /// <param name="invoiceId">ID hóa đơn</param>
    /// <returns>Byte array của file DOCX</returns>
    Task<byte[]> GenerateInvoiceDocxAsync(Guid invoiceId);

    /// <summary>
    /// Tạo file PDF Biên bản trả xe từ template DOCX
    /// </summary>
    /// <param name="returnRecordId">ID biên bản trả xe</param>
    /// <returns>Byte array của file PDF</returns>
    Task<byte[]> GenerateReturnRecordPdfAsync(Guid returnRecordId);

    /// <summary>
    /// Tạo file DOCX Biên bản trả xe từ template
    /// </summary>
    /// <param name="returnRecordId">ID biên bản trả xe</param>
    /// <returns>Byte array của file DOCX</returns>
    Task<byte[]> GenerateReturnRecordDocxAsync(Guid returnRecordId);
}
