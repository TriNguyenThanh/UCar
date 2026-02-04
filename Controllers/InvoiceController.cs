using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UCar.Interfaces;
using UCar.Models;
using UCar.Models.Enums;
using UCar.ViewModels;
using UCar.Data;
using Microsoft.EntityFrameworkCore;

namespace UCar.Controllers
{
    /// <summary>
    /// Invoice management controller - Module 7
    /// Quản lý hóa đơn theo 4 giai đoạn: Deposit, Rental, Surcharge/Penalty, Refund
    /// </summary>
    [Authorize]
    public class InvoiceController : Controller
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IContractService _contractService;
        private readonly UCarDbContext _context;
        private readonly ILogger<InvoiceController> _logger;

        public InvoiceController(
            IInvoiceService invoiceService,
            IContractService contractService,
            UCarDbContext context,
            ILogger<InvoiceController> logger)
        {
            _invoiceService = invoiceService;
            _contractService = contractService;
            _context = context;
            _logger = logger;
        }

        // ===== INDEX - Danh sách hóa đơn =====

        /// <summary>
        /// Hiển thị danh sách hóa đơn với filter và pagination
        /// GET: /Invoice
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(
            InvoiceStatus? status,
            InvoiceType? type,
            DateTime? fromDate,
            DateTime? toDate,
            string? searchTerm,
            int page = 1)
        {
            try
            {
                var (items, totalCount, totalPages) = await _invoiceService.GetInvoicesAsync(
                    status, type, fromDate, toDate, searchTerm, page, 15);

                var viewModel = items.Select(i => new InvoiceListViewModel
                {
                    InvoiceId = i.InvoiceId,
                    InvoiceNumber = i.InvoiceNumber,
                    InvoiceType = i.InvoiceType,
                    ContractCode = i.Contract.ContractCode,
                    CustomerName = i.Customer.FullName,
                    VehiclePlateNo = i.Contract.Vehicle?.PlateNo ?? "N/A",
                    TotalAmount = i.TotalAmount,
                    PreviouslyPaid = i.AmountPaid,
                    AmountDue = i.AmountDue,
                    Status = i.Status,
                    IssuedDate = i.IssuedDate,
                    DueDate = i.DueDate
                }).ToList();

                ViewBag.CurrentStatus = status;
                ViewBag.CurrentType = type;
                ViewBag.FromDate = fromDate;
                ViewBag.ToDate = toDate;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalCount = totalCount;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading invoice list");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách hóa đơn";
                return View(new List<InvoiceListViewModel>());
            }
        }

        // ===== DETAILS - Chi tiết hóa đơn =====

        /// <summary>
        /// Hiển thị chi tiết hóa đơn
        /// GET: /Invoice/Details/{id}
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var invoice = await _invoiceService.GetInvoiceDetailsAsync(id);
                if (invoice == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy hóa đơn";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new InvoiceDetailsViewModel
                {
                    InvoiceId = invoice.InvoiceId,
                    InvoiceNumber = invoice.InvoiceNumber,
                    InvoiceType = invoice.InvoiceType,
                    Status = invoice.Status,
                    IssuedDate = invoice.IssuedDate,
                    DueDate = invoice.DueDate,

                    ContractId = invoice.ContractId,
                    ContractCode = invoice.Contract.ContractCode,
                    VehiclePlateNo = invoice.Contract.Vehicle?.PlateNo ?? "N/A",
                    VehicleModelName = invoice.Contract.Vehicle?.Model?.ModelName ?? "N/A",
                    RentalStart = invoice.Contract.PlannedStart,
                    RentalEnd = invoice.Contract.PlannedEnd,

                    CustomerId = invoice.CustomerId,
                    CustomerName = invoice.Customer.FullName,
                    CustomerPhone = invoice.Customer.UserAccount?.Phone,
                    CustomerEmail = invoice.Customer.UserAccount?.Email,

                    BaseRentalAmount = invoice.BaseRentalAmount,
                    SurchargesTotal = invoice.SurchargesTotal,
                    PenaltiesTotal = invoice.PenaltiesTotal,
                    DiscountAmount = invoice.DiscountAmount,
                    TaxAmount = invoice.TaxAmount,
                    DepositPaid = invoice.DepositPaid,

                    TotalAmount = invoice.TotalAmount,
                    AmountPaid = invoice.AmountPaid,
                    AmountDue = invoice.AmountDue,

                    LineItems = invoice.LineItems.Select(li => new InvoiceLineItemViewModel
                    {
                        LineItemId = li.LineItemId,
                        ItemType = li.ItemType,
                        Description = li.Description,
                        Quantity = li.Quantity,
                        Unit = li.Unit,
                        UnitPrice = li.UnitPrice,
                        Amount = li.Amount,
                        Notes = li.Notes
                    }).ToList(),

                    Adjustments = invoice.Adjustments.Select(a => new InvoiceAdjustmentViewModel
                    {
                        AdjustmentId = a.AdjustmentId,
                        AdjustmentType = a.AdjustmentType,
                        Amount = a.Amount,
                        Reason = a.Reason,
                        AdjustedByName = a.AdjustedByStaff?.FullName ?? "N/A",
                        AdjustedAt = a.AdjustedAt,
                        Notes = a.Notes
                    }).ToList(),

                    IssuedByName = invoice.IssuedByUser?.Username ?? "N/A",
                    Notes = invoice.Notes,
                    InternalNotes = invoice.InternalNotes,
                    CreatedAt = invoice.CreatedAt,
                    UpdatedAt = invoice.UpdatedAt
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading invoice details for {InvoiceId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải chi tiết hóa đơn";
                return RedirectToAction(nameof(Index));
            }
        }

        // ===== PRINT - In hóa đơn =====

        /// <summary>
        /// Hiển thị trang in hóa đơn
        /// GET: /Invoice/Print/{id}
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Print(Guid id)
        {
            try
            {
                var invoice = await _invoiceService.GetInvoiceDetailsAsync(id);
                if (invoice == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy hóa đơn";
                    return RedirectToAction(nameof(Index));
                }

                var invoiceTypeText = invoice.InvoiceType switch
                {
                    InvoiceType.Deposit => "HÓA ĐƠN ĐẶT CỌC",
                    InvoiceType.Rental => "HÓA ĐƠN TIỀN THUÊ XE",
                    InvoiceType.Surcharge => "HÓA ĐƠN PHỤ PHÍ",
                    InvoiceType.Penalty => "HÓA ĐƠN PHẠT",
                    InvoiceType.Refund => "HÓA ĐƠN HOÀN CỌC",
                    _ => "HÓA ĐƠN"
                };

                var rentalDays = (invoice.Contract.PlannedEnd - invoice.Contract.PlannedStart).Days;

                var viewModel = new InvoicePrintViewModel
                {
                    InvoiceNumber = invoice.InvoiceNumber,
                    InvoiceType = invoice.InvoiceType,
                    InvoiceTypeText = invoiceTypeText,
                    Status = invoice.Status,
                    IssuedDate = invoice.IssuedDate,
                    DueDate = invoice.DueDate,

                    CustomerName = invoice.Customer.FullName,
                    CustomerAddress = invoice.Customer.AddressText,
                    CustomerPhone = invoice.Customer.UserAccount?.Phone,
                    CustomerEmail = invoice.Customer.UserAccount?.Email,

                    ContractCode = invoice.Contract.ContractCode,
                    VehiclePlateNo = invoice.Contract.Vehicle?.PlateNo ?? "N/A",
                    VehicleModelName = invoice.Contract.Vehicle?.Model?.ModelName ?? "N/A",
                    RentalStart = invoice.Contract.PlannedStart,
                    RentalEnd = invoice.Contract.PlannedEnd,
                    RentalDays = rentalDays,

                    LineItems = invoice.LineItems.Select(li => new InvoiceLineItemViewModel
                    {
                        ItemType = li.ItemType,
                        Description = li.Description,
                        Quantity = li.Quantity,
                        Unit = li.Unit,
                        UnitPrice = li.UnitPrice,
                        Amount = li.Amount
                    }).ToList(),

                    BaseRentalAmount = invoice.BaseRentalAmount,
                    SurchargesTotal = invoice.SurchargesTotal,
                    PenaltiesTotal = invoice.PenaltiesTotal,
                    DepositPaid = invoice.DepositPaid,
                    Subtotal = invoice.BaseRentalAmount + invoice.SurchargesTotal + invoice.PenaltiesTotal,
                    DiscountAmount = invoice.DiscountAmount,
                    TaxAmount = invoice.TaxAmount,
                    TotalAmount = invoice.TotalAmount,
                    AmountPaid = invoice.AmountPaid,
                    AmountDue = invoice.AmountDue,

                    IssuedByName = invoice.IssuedByUser?.Username ?? "N/A",
                    Notes = invoice.Notes
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading invoice print for {InvoiceId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang in hóa đơn";
                return RedirectToAction(nameof(Index));
            }
        }

        // ===== RECORD PAYMENT - Ghi nhận thanh toán =====

        /// <summary>
        /// Hiển thị form ghi nhận thanh toán
        /// GET: /Invoice/RecordPayment/{id}
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> RecordPayment(Guid id)
        {
            try
            {
                var invoice = await _invoiceService.GetInvoiceDetailsAsync(id);
                if (invoice == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy hóa đơn";
                    return RedirectToAction(nameof(Index));
                }

                if (!CanRecordPayment(invoice))
                {
                    TempData["ErrorMessage"] = "Không thể ghi nhận thanh toán cho hóa đơn này";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var viewModel = new RecordPaymentViewModel
                {
                    InvoiceId = invoice.InvoiceId,
                    InvoiceNumber = invoice.InvoiceNumber,
                    AmountDue = invoice.AmountDue,
                    TotalAmount = invoice.TotalAmount,
                    PreviouslyPaid = invoice.AmountPaid,
                    DueDate = invoice.DueDate,
                    IsOverdue = invoice.Status == InvoiceStatus.Overdue,
                    AmountPaid = invoice.AmountDue  // Pre-fill with full amount
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payment form for {InvoiceId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải form thanh toán";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Xử lý ghi nhận thanh toán
        /// POST: /Invoice/RecordPayment
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordPayment(RecordPaymentViewModel model)
        {
            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            
            if (!ModelState.IsValid)
            {
                if (isAjax)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = string.Join(", ", errors) });
                }
                return View(model);
            }

            try
            {
                // Lấy current user ID (UserAccountId)
                var userId = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var paidBy))
                {
                    if (isAjax)
                        return Json(new { success = false, message = "Không xác định được người dùng hiện tại" });
                    TempData["ErrorMessage"] = "Không xác định được người dùng hiện tại";
                    return View(model);
                }

                var success = await _invoiceService.RecordPaymentAsync(
                    model.InvoiceId,
                    model.AmountPaid,
                    model.PaymentMethod,
                    model.BankRefCode,
                    paidBy,
                    model.Notes);

                if (success)
                {
                    if (isAjax)
                        return Json(new { 
                            success = true, 
                            message = $"Đã ghi nhận thanh toán {model.AmountPaid:N0} VNĐ thành công",
                            redirectUrl = Url.Action(nameof(Details), new { id = model.InvoiceId })
                        });
                    TempData["SuccessMessage"] = $"Đã ghi nhận thanh toán {model.AmountPaid:N0} VNĐ thành công";
                    return RedirectToAction(nameof(Details), new { id = model.InvoiceId });
                }
                else
                {
                    if (isAjax)
                        return Json(new { success = false, message = "Không thể ghi nhận thanh toán" });
                    TempData["ErrorMessage"] = "Không thể ghi nhận thanh toán";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording payment for invoice {InvoiceId}", model.InvoiceId);
                if (isAjax)
                    return Json(new { success = false, message = "Có lỗi xảy ra khi ghi nhận thanh toán: " + ex.Message });
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi ghi nhận thanh toán";
                return View(model);
            }
        }

        // ===== CREATE REFUND INVOICE - Tạo hóa đơn hoàn cọc =====

        /// <summary>
        /// Tạo hóa đơn hoàn cọc cho hợp đồng
        /// POST: /Invoice/CreateRefundInvoice/{contractId}
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRefundInvoice(Guid contractId)
        {
            try
            {
                // Lấy current user ID (UserAccountId)
                var userId = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var issuedBy))
                {
                    TempData["ErrorMessage"] = "Không xác định được người dùng hiện tại";
                    return RedirectToAction(nameof(ByContract), new { contractId });
                }

                var invoiceId = await _invoiceService.CreateRefundInvoiceAsync(contractId, issuedBy);

                if (invoiceId != Guid.Empty)
                {
                    TempData["SuccessMessage"] = "Đã tạo hóa đơn hoàn cọc thành công";
                    return RedirectToAction(nameof(Details), new { id = invoiceId });
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể tạo hóa đơn hoàn cọc. Vui lòng kiểm tra lại điều kiện.";
                    return RedirectToAction(nameof(ByContract), new { contractId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating refund invoice for contract {ContractId}", contractId);
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction(nameof(ByContract), new { contractId });
            }
        }

        // ===== BY CONTRACT - Xem hóa đơn theo hợp đồng =====

        /// <summary>
        /// Hiển thị tất cả hóa đơn của 1 hợp đồng
        /// GET: /Invoice/ByContract/{contractId}
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ByContract(Guid contractId)
        {
            try
            {
                var invoices = await _invoiceService.GetInvoicesByContractAsync(contractId);
                var contract = await _contractService.GetContractDetailsAsync(contractId);

                if (contract == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy hợp đồng";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = invoices.Select(i => new InvoiceListViewModel
                {
                    InvoiceId = i.InvoiceId,
                    InvoiceNumber = i.InvoiceNumber,
                    InvoiceType = i.InvoiceType,
                    ContractCode = contract.ContractCode,
                    CustomerName = contract.CustomerName,
                    VehiclePlateNo = contract.VehiclePlateNo,
                    TotalAmount = i.TotalAmount,
                    AmountDue = i.AmountDue,
                    PreviouslyPaid = i.AmountPaid,
                    Status = i.Status,
                    IssuedDate = i.IssuedDate,
                    DueDate = i.DueDate
                }).OrderBy(i => i.IssuedDate).ToList();

                ViewBag.ContractId = contractId;
                ViewBag.ContractCode = contract.ContractCode;
                ViewBag.CustomerName = contract.CustomerName;
                ViewBag.VehiclePlateNo = contract.VehiclePlateNo;
                ViewBag.VehicleModelName = contract.VehicleModel;

                // Check if can create refund invoice:
                // - Contract must be Completed (đã trả xe)
                // - Must not already have a Refund invoice
                bool hasRefundInvoice = invoices.Any(i => i.InvoiceType == InvoiceType.Refund);
                bool isCompleted = contract.Status == RentalContractStatus.Completed;
                ViewBag.CanCreateRefund = isCompleted && !hasRefundInvoice;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading invoices for contract {ContractId}", contractId);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách hóa đơn";
                return RedirectToAction(nameof(Index));
            }
        }

        // ===== CUSTOMER FACING ACTIONS =====
        // LUỒNG MỚI: Khách hàng không được xem hóa đơn và thanh toán online

        /// <summary>
        /// GET: Invoice/MyInvoices - Customer xem các hóa đơn của mình
        /// LUỒNG MỚI: Không cho phép
        /// </summary>
        [Authorize(Roles = "Customer")]
        [HttpGet]
        public IActionResult MyInvoices()
        {
            // Luồng mới: Khách hàng không được xem hóa đơn online
            TempData["ErrorMessage"] = "Tính năng xem hóa đơn online không còn khả dụng. Vui lòng liên hệ chi nhánh để được hỗ trợ.";
            return RedirectToAction("MyBookings", "Booking");
        }

        /// <summary>
        /// GET: Invoice/Pay/{id} - Customer xem chi tiết hóa đơn để thanh toán
        /// LUỒNG MỚI: Không cho phép thanh toán online
        /// </summary>
        [Authorize(Roles = "Customer")]
        [HttpGet]
        public IActionResult Pay(Guid id)
        {
            // Luồng mới: Khách hàng không được thanh toán online
            TempData["ErrorMessage"] = "Tính năng thanh toán online không còn khả dụng. Vui lòng thanh toán tại quầy khi nhận xe.";
            return RedirectToAction("MyBookings", "Booking");
        }

        /// <summary>
        /// POST: Invoice/SubmitPayment - Customer gửi xác nhận thanh toán
        /// LUỒNG MỚI: Không cho phép thanh toán online
        /// </summary>
        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitPayment(Guid invoiceId, decimal amount, PaymentMethod paymentMethod, string? bankRefCode, string? notes)
        {
            // Luồng mới: Khách hàng không được thanh toán online
            TempData["ErrorMessage"] = "Tính năng thanh toán online không còn khả dụng. Vui lòng thanh toán tại quầy khi nhận xe.";
            return RedirectToAction("MyBookings", "Booking");
        }

        // ===== HELPER METHOD =====

        /// <summary>
        /// Check if invoice can record payment (extension method alternative)
        /// </summary>
        private bool CanRecordPayment(Invoice invoice)
        {
            return invoice.Status == InvoiceStatus.Issued ||
                   invoice.Status == InvoiceStatus.PartiallyPaid ||
                   invoice.Status == InvoiceStatus.Overdue;
        }
    }
}
