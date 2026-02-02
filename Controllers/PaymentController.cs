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
}
