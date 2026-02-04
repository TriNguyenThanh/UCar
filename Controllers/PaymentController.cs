using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UCar.Interfaces;
using UCar.ViewModels.Payment;

namespace UCar.Controllers;

/// <summary>
/// Controller quản lý thanh toán
/// </summary>
[Authorize(Roles = "Admin,BranchManager,Staff")]
public class PaymentController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Trang thanh toán cho hợp đồng
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Payment(Guid contractId)
    {
        var model = await _paymentService.GetPaymentInfoAsync(contractId);
        if (model == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy thông tin thanh toán";
            return RedirectToAction("Return", "Handover");
        }

        return View(model);
    }

    /// <summary>
    /// Xử lý thanh toán
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessPayment(PaymentProcessViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var info = await _paymentService.GetPaymentInfoAsync(model.ContractId);
            if (info != null)
            {
                model.ContractCode = info.ContractCode;
                model.CustomerName = info.CustomerName;
                model.GrandTotal = info.GrandTotal;
                model.DepositAmount = info.DepositAmount;
                model.AmountDue = info.AmountDue;
                model.AmountPaid = info.AmountPaid;
            }
            return View("Payment", model);
        }

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _paymentService.ProcessPaymentAsync(model, userId);

        if (result.Success)
        {
            TempData["SuccessMessage"] = "Thanh toán thành công!";
            return RedirectToAction("ReturnDocument", "Handover", new { contractId = model.ContractId });
        }

        TempData["ErrorMessage"] = result.Errors.FirstOrDefault() ?? "Có lỗi xảy ra";
        return RedirectToAction("Payment", new { contractId = model.ContractId });
    }

    /// <summary>
    /// Trang hoàn tiền
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Refund(Guid contractId)
    {
        var model = await _paymentService.GetRefundInfoAsync(contractId);
        if (model == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy thông tin hoàn tiền";
            return RedirectToAction("Return", "Handover");
        }

        return View(model);
    }

    /// <summary>
    /// Xử lý hoàn tiền
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessRefund(RefundProcessViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var info = await _paymentService.GetRefundInfoAsync(model.ContractId);
            return View("Refund", info);
        }

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _paymentService.ProcessRefundAsync(model, userId);

        if (result.Success)
        {
            TempData["SuccessMessage"] = "Hoàn tiền thành công!";
            return RedirectToAction("ReturnDocument", "Handover", new { contractId = model.ContractId });
        }

        TempData["ErrorMessage"] = result.Errors.FirstOrDefault() ?? "Có lỗi xảy ra";
        return RedirectToAction("Refund", new { contractId = model.ContractId });
    }

    /// <summary>
    /// Lịch sử thanh toán của hợp đồng
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> History(Guid contractId)
    {
        var history = await _paymentService.GetPaymentHistoryAsync(contractId);
        return View(history);
    }


    public record TransactionWebhookRequest (decimal transferAmount, string code);
    [AllowAnonymous]
    //api webhook
    [HttpPost("/api/webhook/confirm-payment")]
    public async Task<IActionResult> WebHookConfirmPayment([FromBody] TransactionWebhookRequest request)
        {
            var api_key = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            api_key = api_key?.Replace("Apikey ", ""); // Loại bỏ "Apikey " nếu có
            var expected_api_key = Environment.GetEnvironmentVariable("WEBHOOK_API_KEY") ?? "123456789";

            if (api_key != expected_api_key)
            {
                return Unauthorized(new
                {
                    status = "failed",
                    message = "Invalid API Key",
                    success = false
                });
            }

            try
            {
                // Tìm mã invoice dạng INV001, INV-001, RF001, RF-001, etc.
                var match = System.Text.RegularExpressions.Regex.Match(
                    request.code,
                    @"(INV)[-]?(\d{6})",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );

                if (!match.Success)
                {
                    return BadRequest(new
                    {
                        status = "failed",
                        message = "Invoice ID not found in transfer content",
                        success = false,
                        hint = "Content should contain invoice ID (e.g., 'INV-000001' or 'INV000001')"
                    });
                }

                // Tạo mã chuẩn: INV-000001
                var prefix = match.Groups[1].Value.ToUpper();
                var number = match.Groups[2].Value;
                var invoiceID = $"{prefix}-{number}";

                var paymentMethod = "TRANSFER"; // Mặc định là chuyển khoản
                // string employeeID = extractCode[1].Trim();
                decimal amount = request.transferAmount;

                // Lấy thông tin hóa đơn
                // var invoice = await _context.Invoices
                //     .FirstOrDefaultAsync(i => i.InvoiceID == invoiceID); //có hóa đơn thì bỏ cmt

                // if (invoice == null)
                // {
                //     return BadRequest(new
                //     {
                //         status = "failed",
                //         message = "Invoice not found",
                //         success = false
                //     });
                // }

                // Kiểm tra số tiền thanh toán
                // if (amount <= 0 || amount != invoice.TotalAmount)
                // {
                //     return BadRequest(new
                //     {
                //         status = "failed",
                //         message = "Invalid payment amount",
                //         success = false
                //     });
                // }

                // Gọi SP để xác nhận thanh toán
                // var result = await _context.ConfirmPaymentSP(invoiceID, paymentMethod, employeeID: "");

                // if (result != null && result.Status == "PAYMENT_CONFIRMED")
                // {
                //     return Ok(new
                //     {
                //         status = "success",
                //         message = "Payment confirmed successfully",
                //         success = true,
                //         invoiceID = invoiceID
                //     });
                // }
                // else
                // {
                //     return BadRequest(new
                //     {
                //         status = "failed",
                //         message = result?.Status ?? "Cannot confirm payment",
                //         success = false
                //     });
                // }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "error",
                    message = ex.Message,
                    success = false
                });
            }

            return Ok(new
            {
                status = "success",
                message = "Payment confirmed successfully",
                success = true,
            });
        }
}
