using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models;
using UCar.Models.Enums;

namespace UCar.Services;

/// <summary>
/// Service xử lý template Word/DOCX cho Hợp đồng và Hóa đơn
/// Sử dụng thư viện OpenXML SDK để đọc template và thay thế placeholder
/// 
/// CÁCH SỬ DỤNG TEMPLATE:
/// 1. Tạo file Word (.docx) với các placeholder dạng {{TenTruong}}
/// 2. Đặt file vào thư mục wwwroot/templates/
/// 3. Service sẽ tự động thay thế placeholder bằng dữ liệu thực
/// </summary>
public class DocumentTemplateService : IDocumentTemplateService
{
    private readonly UCarDbContext _context;
    private readonly ILogger<DocumentTemplateService> _logger;
    private readonly IWebHostEnvironment _environment;

    private string TemplatesPath => Path.Combine(_environment.WebRootPath, "templates");

    public DocumentTemplateService(
        UCarDbContext context,
        ILogger<DocumentTemplateService> logger,
        IWebHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _environment = environment;
    }

    #region Contract Documents

    public async Task<byte[]> GenerateContractDocxAsync(Guid contractId)
    {
        var contract = await _context.RentalContracts
            .Include(c => c.Customer)
                .ThenInclude(cust => cust.UserAccount)
            .Include(c => c.Vehicle)
                .ThenInclude(v => v.Model)
                    .ThenInclude(m => m.VehicleType)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);

        if (contract == null)
            throw new InvalidOperationException($"Contract {contractId} not found");

        var templatePath = Path.Combine(TemplatesPath, "HopDongThueXe.docx");
        
        if (!File.Exists(templatePath))
        {
            _logger.LogWarning("Template not found: {Path}. Creating default.", templatePath);
            CreateDefaultContractTemplate(templatePath);
        }

        var customer = contract.Customer;
        var user = customer.UserAccount;
        var vehicle = contract.Vehicle;
        var model = vehicle.Model;

        var replacements = new Dictionary<string, string>
        {
            { "{{ContractCode}}", contract.ContractCode },
            { "{{PrintDate}}", DateTime.Now.ToString("dd/MM/yyyy") },
            { "{{CompanyName}}", "CÔNG TY TNHH CHO THUÊ XE UCAR" },
            { "{{CompanyAddress}}", "123 Đường ABC, Quận XYZ, TP. Hồ Chí Minh" },
            { "{{CompanyPhone}}", "1900 1234" },
            { "{{CompanyEmail}}", "contact@ucar.vn" },
            { "{{CompanyTaxCode}}", "0123456789" },
            
            // Customer
            { "{{CustomerName}}", customer.FullName },
            { "{{CustomerPhone}}", user.Phone ?? "N/A" },
            { "{{CustomerEmail}}", user.Email ?? "N/A" },
            { "{{CustomerAddress}}", customer.AddressText ?? "N/A" },
            { "{{CustomerDob}}", customer.Dob?.ToString("dd/MM/yyyy") ?? "N/A" },
            
            // Vehicle
            { "{{VehiclePlateNo}}", vehicle.PlateNo },
            { "{{VehicleBrand}}", model.Make },
            { "{{VehicleModel}}", model.ModelName },
            { "{{VehicleType}}", model.VehicleType?.TypeName ?? "N/A" },
            { "{{VehicleYear}}", vehicle.ManufactureYear.ToString() },
            { "{{VehicleColor}}", vehicle.Color ?? "N/A" },
            
            // Time & Location
            { "{{PlannedStart}}", contract.PlannedStart.ToString("HH:mm dd/MM/yyyy") },
            { "{{PlannedEnd}}", contract.PlannedEnd.ToString("HH:mm dd/MM/yyyy") },
            { "{{RentalDays}}", contract.RentalDays.ToString() },
            { "{{PickupLocation}}", contract.PickupLocation ?? "N/A" },
            { "{{ReturnLocation}}", contract.ReturnLocation ?? "N/A" },
            
            // Cost
            { "{{DailyRate}}", contract.SnapshotBaseDailyPrice.ToString("N0") },
            { "{{RentalAmount}}", contract.RentalAmount.ToString("N0") },
            { "{{ResponsibilityDeposit}}", contract.ResponsibilityDeposit.ToString("N0") },
            { "{{RentalDeposit}}", contract.RentalDeposit.ToString("N0") },
            { "{{DepositAmount}}", (contract.ResponsibilityDeposit + contract.RentalDeposit).ToString("N0") },
            { "{{TotalAmount}}", contract.TotalAmountFinal.ToString("N0") },
            
            // Status
            { "{{Status}}", GetContractStatusText(contract.Status) },
            { "{{CreatedAt}}", contract.CreatedAt.ToString("dd/MM/yyyy") },
        };

        return await ProcessTemplateAsync(templatePath, replacements);
    }

    public async Task<byte[]> GenerateContractPdfAsync(Guid contractId)
    {
        _logger.LogWarning("PDF generation not supported. Returning DOCX.");
        return await GenerateContractDocxAsync(contractId);
    }

    #endregion

    #region Handover Documents

    public async Task<byte[]> GenerateHandoverDocxAsync(Guid handoverRecordId)
    {
        var handover = await _context.HandoverRecords
            .Include(h => h.RentalContract)
                .ThenInclude(c => c.Customer)
                    .ThenInclude(cust => cust.UserAccount)
            .Include(h => h.RentalContract)
                .ThenInclude(c => c.Vehicle)
                    .ThenInclude(v => v.Model)
            .Include(h => h.Accessories)
            .Include(h => h.HandedOverByUser)
            .FirstOrDefaultAsync(h => h.HandoverId == handoverRecordId);

        if (handover == null)
            throw new InvalidOperationException($"Handover record {handoverRecordId} not found");

        var templatePath = Path.Combine(TemplatesPath, "BienBanGiaoXe.docx");
        
        if (!File.Exists(templatePath))
        {
            _logger.LogWarning("Template not found: {Path}. Creating default.", templatePath);
            CreateDefaultHandoverTemplate(templatePath);
        }

        var contract = handover.RentalContract;
        var customer = contract.Customer;
        var vehicle = contract.Vehicle;

        var replacements = new Dictionary<string, string>
        {
            { "{{HandoverCode}}", $"BB-{handover.HandoverId.ToString()[..8].ToUpper()}" },
            { "{{ContractCode}}", contract.ContractCode },
            { "{{HandoverDate}}", handover.HandedAt.ToString("dd/MM/yyyy HH:mm") },
            
            { "{{CustomerName}}", customer.FullName },
            { "{{CustomerPhone}}", customer.UserAccount.Phone ?? "N/A" },
            
            { "{{VehiclePlateNo}}", vehicle.PlateNo },
            { "{{VehicleBrand}}", vehicle.Model.Make },
            { "{{VehicleModel}}", vehicle.Model.ModelName },
            
            { "{{FuelLevel}}", handover.FuelLevelOut.ToString("N0") + "%" },
            { "{{Odometer}}", handover.OdoKmOut.ToString("N0") },
            { "{{ExteriorCondition}}", handover.ExteriorCondition ?? "Tốt" },
            { "{{InteriorCondition}}", handover.InteriorCondition ?? "Tốt" },
            
            { "{{Accessories}}", string.Join(", ", handover.Accessories.Select(a => a.AccessoryName)) },
            { "{{StaffName}}", handover.HandedOverByUser?.Username ?? "N/A" },
            { "{{Notes}}", handover.Note ?? "" },
        };

        return await ProcessTemplateAsync(templatePath, replacements);
    }

    public async Task<byte[]> GenerateHandoverPdfAsync(Guid handoverRecordId)
    {
        _logger.LogWarning("PDF generation not supported. Returning DOCX.");
        return await GenerateHandoverDocxAsync(handoverRecordId);
    }

    #endregion

    #region Invoice Documents

    public async Task<byte[]> GenerateInvoiceDocxAsync(Guid invoiceId)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Contract)
                .ThenInclude(c => c.Customer)
                    .ThenInclude(cust => cust.UserAccount)
            .Include(i => i.Contract)
                .ThenInclude(c => c.Vehicle)
                    .ThenInclude(v => v.Model)
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

        if (invoice == null)
            throw new InvalidOperationException($"Invoice {invoiceId} not found");

        var templatePath = Path.Combine(TemplatesPath, "HoaDon.docx");
        
        if (!File.Exists(templatePath))
        {
            _logger.LogWarning("Template not found: {Path}. Creating default.", templatePath);
            CreateDefaultInvoiceTemplate(templatePath);
        }

        var contract = invoice.Contract;
        var customer = contract.Customer;
        var vehicle = contract.Vehicle;

        var replacements = new Dictionary<string, string>
        {
            { "{{InvoiceNumber}}", invoice.InvoiceNumber },
            { "{{InvoiceType}}", GetInvoiceTypeText(invoice.InvoiceType) },
            { "{{IssuedDate}}", invoice.IssuedDate.ToString("dd/MM/yyyy") },
            { "{{DueDate}}", invoice.DueDate?.ToString("dd/MM/yyyy") ?? "N/A" },
            { "{{ContractCode}}", contract.ContractCode },
            
            { "{{CustomerName}}", customer.FullName },
            { "{{CustomerPhone}}", customer.UserAccount.Phone ?? "N/A" },
            { "{{CustomerAddress}}", customer.AddressText ?? "N/A" },
            
            { "{{VehiclePlateNo}}", vehicle.PlateNo },
            { "{{VehicleInfo}}", $"{vehicle.Model.Make} {vehicle.Model.ModelName}" },
            
            { "{{TotalAmount}}", invoice.TotalAmount.ToString("N0") },
            { "{{AmountPaid}}", invoice.AmountPaid.ToString("N0") },
            { "{{AmountDue}}", invoice.AmountDue.ToString("N0") },
            { "{{Status}}", GetInvoiceStatusText(invoice.Status) },
            { "{{Notes}}", invoice.Notes ?? "" },
        };

        return await ProcessTemplateAsync(templatePath, replacements);
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(Guid invoiceId)
    {
        _logger.LogWarning("PDF generation not supported. Returning DOCX.");
        return await GenerateInvoiceDocxAsync(invoiceId);
    }

    #endregion

    #region Return Record Documents

    public async Task<byte[]> GenerateReturnRecordDocxAsync(Guid returnRecordId)
    {
        var returnRecord = await _context.ReturnRecords
            .Include(r => r.RentalContract)
                .ThenInclude(c => c.Customer)
                    .ThenInclude(cust => cust.UserAccount)
            .Include(r => r.RentalContract)
                .ThenInclude(c => c.Vehicle)
                    .ThenInclude(v => v.Model)
            .Include(r => r.ReceivedByUser)
            .FirstOrDefaultAsync(r => r.ReturnId == returnRecordId);

        if (returnRecord == null)
            throw new InvalidOperationException($"Return record {returnRecordId} not found");

        var templatePath = Path.Combine(TemplatesPath, "BienBanTraXe.docx");
        
        if (!File.Exists(templatePath))
        {
            _logger.LogWarning("Template not found: {Path}. Creating default.", templatePath);
            CreateDefaultReturnRecordTemplate(templatePath);
        }

        var contract = returnRecord.RentalContract;
        var customer = contract.Customer;
        var vehicle = contract.Vehicle;

        var replacements = new Dictionary<string, string>
        {
            { "{{ReturnCode}}", $"TRX-{returnRecord.ReturnId.ToString()[..8].ToUpper()}" },
            { "{{ContractCode}}", contract.ContractCode },
            { "{{ReturnDate}}", returnRecord.ReturnedAt.ToString("dd/MM/yyyy HH:mm") },
            
            { "{{CustomerName}}", customer.FullName },
            { "{{CustomerPhone}}", customer.UserAccount.Phone ?? "N/A" },
            
            { "{{VehiclePlateNo}}", vehicle.PlateNo },
            { "{{VehicleInfo}}", $"{vehicle.Model.Make} {vehicle.Model.ModelName}" },
            
            { "{{FuelLevel}}", returnRecord.FuelLevelIn.ToString("N0") + "%" },
            { "{{Odometer}}", returnRecord.OdoKmIn.ToString("N0") },
            { "{{ExteriorCondition}}", returnRecord.ExteriorCondition ?? "Tốt" },
            { "{{InteriorCondition}}", returnRecord.InteriorCondition ?? "Tốt" },
            
            { "{{OvertimeHours}}", returnRecord.OvertimeHours.ToString("N1") },
            { "{{FuelShortage}}", returnRecord.FuelShortage.ToString("N1") + "%" },
            
            { "{{StaffName}}", returnRecord.ReceivedByUser?.Username ?? "N/A" },
            { "{{Notes}}", returnRecord.Note ?? "" },
        };

        return await ProcessTemplateAsync(templatePath, replacements);
    }

    public async Task<byte[]> GenerateReturnRecordPdfAsync(Guid returnRecordId)
    {
        _logger.LogWarning("PDF generation not supported. Returning DOCX.");
        return await GenerateReturnRecordDocxAsync(returnRecordId);
    }

    #endregion

    #region Template Processing

    private async Task<byte[]> ProcessTemplateAsync(string templatePath, Dictionary<string, string> replacements)
    {
        byte[] templateBytes = await File.ReadAllBytesAsync(templatePath);
        
        using var memoryStream = new MemoryStream();
        memoryStream.Write(templateBytes, 0, templateBytes.Length);
        memoryStream.Position = 0;

        using (var wordDocument = WordprocessingDocument.Open(memoryStream, true))
        {
            var body = wordDocument.MainDocumentPart?.Document?.Body;
            if (body == null)
                throw new InvalidOperationException("Invalid DOCX template");

            foreach (var text in body.Descendants<Text>())
            {
                foreach (var replacement in replacements)
                {
                    if (text.Text.Contains(replacement.Key))
                    {
                        text.Text = text.Text.Replace(replacement.Key, replacement.Value);
                    }
                }
            }

            wordDocument.Save();
        }

        return memoryStream.ToArray();
    }

    #endregion

    #region Default Templates

    private void CreateDefaultContractTemplate(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());

        AddCenteredParagraph(body, "CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM", true);
        AddCenteredParagraph(body, "Độc lập - Tự do - Hạnh phúc", false);
        AddCenteredParagraph(body, "----o0o----", false);
        AddParagraph(body, "");
        AddCenteredParagraph(body, "HỢP ĐỒNG THUÊ XE TỰ LÁI", true, "32");
        AddCenteredParagraph(body, "Số: {{ContractCode}}", false);
        AddParagraph(body, "");

        AddParagraph(body, "BÊN CHO THUÊ (BÊN A):", true);
        AddParagraph(body, "Công ty: {{CompanyName}}");
        AddParagraph(body, "Địa chỉ: {{CompanyAddress}}");
        AddParagraph(body, "Điện thoại: {{CompanyPhone}}");
        AddParagraph(body, "");

        AddParagraph(body, "BÊN THUÊ (BÊN B):", true);
        AddParagraph(body, "Họ và tên: {{CustomerName}}");
        AddParagraph(body, "Địa chỉ: {{CustomerAddress}}");
        AddParagraph(body, "Điện thoại: {{CustomerPhone}}");
        AddParagraph(body, "");

        AddParagraph(body, "ĐIỀU 1: THÔNG TIN XE", true);
        AddParagraph(body, "- Biển số: {{VehiclePlateNo}}");
        AddParagraph(body, "- Xe: {{VehicleBrand}} {{VehicleModel}}");
        AddParagraph(body, "- Loại: {{VehicleType}}");
        AddParagraph(body, "");

        AddParagraph(body, "ĐIỀU 2: THỜI GIAN", true);
        AddParagraph(body, "- Từ: {{PlannedStart}}");
        AddParagraph(body, "- Đến: {{PlannedEnd}}");
        AddParagraph(body, "- Số ngày: {{RentalDays}} ngày");
        AddParagraph(body, "");

        AddParagraph(body, "ĐIỀU 3: CHI PHÍ", true);
        AddParagraph(body, "- Giá thuê: {{DailyRate}} VNĐ/ngày");
        AddParagraph(body, "- Tiền thuê: {{RentalAmount}} VNĐ");
        AddParagraph(body, "- Tiền cọc: {{DepositAmount}} VNĐ");
        AddParagraph(body, "- TỔNG: {{TotalAmount}} VNĐ");
        AddParagraph(body, "");

        AddParagraph(body, "");
        AddParagraph(body, "    BÊN A                                    BÊN B", true);
        AddParagraph(body, "    (Ký tên)                                 (Ký tên)");

        mainPart.Document.Save();
        _logger.LogInformation("Created default contract template: {Path}", path);
    }

    private void CreateDefaultHandoverTemplate(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());

        AddCenteredParagraph(body, "BIÊN BẢN GIAO XE", true, "32");
        AddCenteredParagraph(body, "Số: {{HandoverCode}}", false);
        AddCenteredParagraph(body, "Hợp đồng: {{ContractCode}}", false);
        AddParagraph(body, "");
        AddParagraph(body, "Ngày giao: {{HandoverDate}}");
        AddParagraph(body, "");

        AddParagraph(body, "KHÁCH HÀNG:", true);
        AddParagraph(body, "- Họ tên: {{CustomerName}}");
        AddParagraph(body, "- ĐT: {{CustomerPhone}}");
        AddParagraph(body, "");

        AddParagraph(body, "XE:", true);
        AddParagraph(body, "- Biển số: {{VehiclePlateNo}}");
        AddParagraph(body, "- Xe: {{VehicleBrand}} {{VehicleModel}}");
        AddParagraph(body, "");

        AddParagraph(body, "TÌNH TRẠNG:", true);
        AddParagraph(body, "- Nhiên liệu: {{FuelLevel}}");
        AddParagraph(body, "- Số km: {{Odometer}} km");
        AddParagraph(body, "- Ngoại thất: {{ExteriorCondition}}");
        AddParagraph(body, "- Nội thất: {{InteriorCondition}}");
        AddParagraph(body, "");

        AddParagraph(body, "PHỤ KIỆN: {{Accessories}}");
        AddParagraph(body, "GHI CHÚ: {{Notes}}");
        AddParagraph(body, "");
        AddParagraph(body, "");
        AddParagraph(body, "    NHÂN VIÊN                                KHÁCH HÀNG", true);
        AddParagraph(body, "    {{StaffName}}                            {{CustomerName}}");

        mainPart.Document.Save();
        _logger.LogInformation("Created default handover template: {Path}", path);
    }

    private void CreateDefaultInvoiceTemplate(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());

        AddCenteredParagraph(body, "HÓA ĐƠN", true, "32");
        AddCenteredParagraph(body, "{{InvoiceType}}", false, "28");
        AddCenteredParagraph(body, "Số: {{InvoiceNumber}}", false);
        AddParagraph(body, "");

        AddParagraph(body, "Ngày: {{IssuedDate}}");
        AddParagraph(body, "Hạn TT: {{DueDate}}");
        AddParagraph(body, "Hợp đồng: {{ContractCode}}");
        AddParagraph(body, "");

        AddParagraph(body, "KHÁCH HÀNG:", true);
        AddParagraph(body, "- {{CustomerName}}");
        AddParagraph(body, "- ĐT: {{CustomerPhone}}");
        AddParagraph(body, "");

        AddParagraph(body, "XE: {{VehiclePlateNo}} - {{VehicleInfo}}");
        AddParagraph(body, "");

        AddParagraph(body, "TỔNG: {{TotalAmount}} VNĐ", true);
        AddParagraph(body, "ĐÃ TRẢ: {{AmountPaid}} VNĐ");
        AddParagraph(body, "CÒN LẠI: {{AmountDue}} VNĐ", true);
        AddParagraph(body, "");
        AddParagraph(body, "Trạng thái: {{Status}}");
        AddParagraph(body, "Ghi chú: {{Notes}}");

        mainPart.Document.Save();
        _logger.LogInformation("Created default invoice template: {Path}", path);
    }

    private void CreateDefaultReturnRecordTemplate(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());

        AddCenteredParagraph(body, "BIÊN BẢN TRẢ XE", true, "32");
        AddCenteredParagraph(body, "Số: {{ReturnCode}}", false);
        AddCenteredParagraph(body, "Hợp đồng: {{ContractCode}}", false);
        AddParagraph(body, "");
        AddParagraph(body, "Ngày trả: {{ReturnDate}}");
        AddParagraph(body, "");

        AddParagraph(body, "KHÁCH HÀNG:", true);
        AddParagraph(body, "- {{CustomerName}}");
        AddParagraph(body, "- ĐT: {{CustomerPhone}}");
        AddParagraph(body, "");

        AddParagraph(body, "XE: {{VehiclePlateNo}} - {{VehicleInfo}}");
        AddParagraph(body, "");

        AddParagraph(body, "TÌNH TRẠNG:", true);
        AddParagraph(body, "- Nhiên liệu: {{FuelLevel}}");
        AddParagraph(body, "- Số km: {{Odometer}} km");
        AddParagraph(body, "- Ngoại thất: {{ExteriorCondition}}");
        AddParagraph(body, "- Nội thất: {{InteriorCondition}}");
        AddParagraph(body, "");

        AddParagraph(body, "PHÍ PHÁT SINH: {{ExtraCharges}} VNĐ");
        AddParagraph(body, "HOÀN LẠI: {{RefundAmount}} VNĐ");
        AddParagraph(body, "GHI CHÚ: {{Notes}}");
        AddParagraph(body, "");
        AddParagraph(body, "");
        AddParagraph(body, "    NHÂN VIÊN                                KHÁCH HÀNG", true);
        AddParagraph(body, "    {{StaffName}}                            {{CustomerName}}");

        mainPart.Document.Save();
        _logger.LogInformation("Created default return record template: {Path}", path);
    }

    #endregion

    #region Helpers

    private void AddParagraph(Body body, string text, bool bold = false, string fontSize = "24")
    {
        var paragraph = new Paragraph();
        var run = new Run();
        var props = new RunProperties();
        if (bold) props.Append(new Bold());
        props.Append(new FontSize { Val = fontSize });
        run.Append(props);
        run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        paragraph.Append(run);
        body.Append(paragraph);
    }

    private void AddCenteredParagraph(Body body, string text, bool bold, string fontSize = "24")
    {
        var paragraph = new Paragraph();
        paragraph.Append(new ParagraphProperties(new Justification { Val = JustificationValues.Center }));
        var run = new Run();
        var props = new RunProperties();
        if (bold) props.Append(new Bold());
        props.Append(new FontSize { Val = fontSize });
        run.Append(props);
        run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        paragraph.Append(run);
        body.Append(paragraph);
    }

    private string GetContractStatusText(RentalContractStatus status) => status switch
    {
        RentalContractStatus.Draft => "Bản nháp",
        RentalContractStatus.PendingSigning => "Chờ ký",
        RentalContractStatus.Active => "Đang hoạt động",
        RentalContractStatus.InProgress => "Đang thuê",
        RentalContractStatus.Completed => "Hoàn thành",
        RentalContractStatus.Cancelled => "Đã hủy",
        _ => status.ToString()
    };

    private string GetInvoiceTypeText(InvoiceType type) => type switch
    {
        InvoiceType.Deposit => "HÓA ĐƠN ĐẶT CỌC",
        InvoiceType.Rental => "HÓA ĐƠN TIỀN THUÊ",
        InvoiceType.Surcharge => "HÓA ĐƠN PHỤ PHÍ",
        InvoiceType.Penalty => "HÓA ĐƠN PHẠT",
        InvoiceType.Refund => "HÓA ĐƠN HOÀN CỌC",
        InvoiceType.Delivery => "HÓA ĐƠN GIAO XE",
        InvoiceType.Return => "HÓA ĐƠN TRẢ XE",
        _ => "HÓA ĐƠN"
    };

    private string GetInvoiceStatusText(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Issued => "Đã phát hành",
        InvoiceStatus.Paid => "Đã thanh toán",
        InvoiceStatus.PartiallyPaid => "Thanh toán một phần",
        InvoiceStatus.Overdue => "Quá hạn",
        InvoiceStatus.Cancelled => "Đã hủy",
        _ => status.ToString()
    };

    #endregion
}
