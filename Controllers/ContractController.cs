using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UCar.Interfaces;
using UCar.Models.Enums;
using UCar.ViewModels.Contract;

namespace UCar.Controllers;

/// <summary>
/// Controller quản lý hợp đồng thuê xe
/// Tương ứng DFD 5.0: Quản lý hợp đồng thuê
/// </summary>
[Authorize]
public class ContractController : Controller
{
    private readonly IContractService _contractService;
    private readonly ILogger<ContractController> _logger;

    public ContractController(IContractService contractService, ILogger<ContractController> logger)
    {
        _contractService = contractService;
        _logger = logger;
    }

    #region Helpers

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null && Guid.TryParse(claim.Value, out Guid id)) return id;
        return Guid.Empty;
    }

    private bool IsAdminOrStaff => User.IsInRole("Admin") || User.IsInRole("Staff");

    #endregion

    #region 4.1 Danh sách hợp đồng

    /// <summary>
    /// Trang chủ - điều hướng theo role
    /// </summary>
    public IActionResult Index()
    {
        if (IsAdminOrStaff)
        {
            return RedirectToAction(nameof(Manage));
        }
        return RedirectToAction(nameof(MyContracts));
    }

    /// <summary>
    /// Danh sách hợp đồng (Admin/Staff)
    /// GET: /Contract/Manage
    /// </summary>
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Manage(ContractSearchViewModel filter)
    {
        var result = await _contractService.GetContractsAsync(filter);
        return View(result);
    }

    /// <summary>
    /// Danh sách hợp đồng của tôi (Customer)
    /// GET: /Contract/MyContracts
    /// </summary>
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> MyContracts(ContractSearchViewModel filter)
    {
        var userId = GetCurrentUserId();
        var result = await _contractService.GetMyContractsAsync(userId, filter);
        return View(result);
    }

    #endregion

    #region 4.2 Tạo hợp đồng

    /// <summary>
    /// Form tạo hợp đồng
    /// GET: /Contract/Create?bookingId=...
    /// </summary>
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Create(Guid? bookingId)
    {
        var model = new ContractCreateViewModel();
        var options = await _contractService.GetCreateOptionsAsync();
        ViewBag.Options = options;

        // Nếu tạo từ booking
        if (bookingId.HasValue)
        {
            var bookingInfo = await _contractService.GetBookingForContractAsync(bookingId.Value);
            if (bookingInfo == null)
            {
                TempData["Error"] = "Không tìm thấy đơn đặt xe hoặc đơn đã có hợp đồng.";
                return RedirectToAction(nameof(Manage));
            }

            model.BookingId = bookingInfo.BookingId;
            model.CustomerId = bookingInfo.CustomerId;
            model.CustomerName = bookingInfo.CustomerName;
            model.CustomerPhone = bookingInfo.CustomerPhone;
            model.VehicleId = bookingInfo.VehicleId;
            model.VehiclePlateNo = bookingInfo.VehiclePlateNo;
            model.VehicleModel = bookingInfo.VehicleModel;
            model.PlannedStart = bookingInfo.StartAt;
            model.PlannedEnd = bookingInfo.EndAt;
            model.PriceId = bookingInfo.PriceId;
            model.UnitPrice = bookingInfo.UnitPrice;
            model.DepositAmount = bookingInfo.DepositSuggest;
            model.Terms = options.DefaultTerms;

            // Calculate
            var (days, rentalAmount, total) = _contractService.CalculateRentalAmount(
                bookingInfo.StartAt, bookingInfo.EndAt, bookingInfo.UnitPrice, 0);
            model.RentalDays = days;
            model.RentalAmount = rentalAmount;
            model.TotalAmount = total;

            ViewBag.BookingInfo = bookingInfo;
        }
        else
        {
            // Walk-in
            model.IsWalkIn = true;
            model.PlannedStart = DateTime.Now.AddHours(1);
            model.PlannedEnd = DateTime.Now.AddDays(1);
            model.Terms = options.DefaultTerms;
        }

        return View(model);
    }

    /// <summary>
    /// Xử lý tạo hợp đồng
    /// POST: /Contract/Create
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Create(ContractCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var options = await _contractService.GetCreateOptionsAsync();
            ViewBag.Options = options;
            return View(model);
        }

        try
        {
            var userId = GetCurrentUserId();
            var contractId = await _contractService.CreateContractAsync(model, userId);

            TempData["Success"] = model.SaveAsDraft 
                ? "Đã lưu bản nháp hợp đồng thành công!" 
                : "Đã tạo hợp đồng thành công!";

            return RedirectToAction(nameof(Details), new { id = contractId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            var options = await _contractService.GetCreateOptionsAsync();
            ViewBag.Options = options;
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contract");
            ModelState.AddModelError("", "Có lỗi xảy ra khi tạo hợp đồng. Vui lòng thử lại.");
            var options = await _contractService.GetCreateOptionsAsync();
            ViewBag.Options = options;
            return View(model);
        }
    }

    #endregion

    #region 4.3 Chi tiết hợp đồng

    /// <summary>
    /// Xem chi tiết hợp đồng
    /// GET: /Contract/Details/{id}
    /// </summary>
    public async Task<IActionResult> Details(Guid id)
    {
        var contract = await _contractService.GetContractDetailsAsync(id);
        if (contract == null)
        {
            TempData["Error"] = "Không tìm thấy hợp đồng.";
            return RedirectToAction(nameof(Index));
        }

        // Check access for customers
        if (User.IsInRole("Customer"))
        {
            var userId = GetCurrentUserId();
            // Verify ownership via customer relationship
            // This should be enhanced with proper customer ID check
        }

        return View(contract);
    }

    #endregion

    #region 4.4 Cập nhật hợp đồng

    /// <summary>
    /// Form sửa hợp đồng
    /// GET: /Contract/Edit/{id}
    /// </summary>
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await _contractService.GetContractForEditAsync(id);
        if (model == null)
        {
            TempData["Error"] = "Không tìm thấy hợp đồng hoặc hợp đồng không thể chỉnh sửa.";
            return RedirectToAction(nameof(Manage));
        }

        var options = await _contractService.GetCreateOptionsAsync();
        ViewBag.Options = options;

        return View(model);
    }

    /// <summary>
    /// Xử lý cập nhật hợp đồng
    /// POST: /Contract/Edit/{id}
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Edit(Guid id, ContractEditViewModel model)
    {
        if (id != model.ContractId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            var options = await _contractService.GetCreateOptionsAsync();
            ViewBag.Options = options;
            return View(model);
        }

        try
        {
            var userId = GetCurrentUserId();
            var success = await _contractService.UpdateContractAsync(model, userId);

            if (!success)
            {
                TempData["Error"] = "Không thể cập nhật hợp đồng.";
                return RedirectToAction(nameof(Manage));
            }

            TempData["Success"] = "Đã cập nhật hợp đồng thành công!";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            var options = await _contractService.GetCreateOptionsAsync();
            ViewBag.Options = options;
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating contract {ContractId}", id);
            ModelState.AddModelError("", "Có lỗi xảy ra. Vui lòng thử lại.");
            var options = await _contractService.GetCreateOptionsAsync();
            ViewBag.Options = options;
            return View(model);
        }
    }

    #endregion

    #region 4.5 Ký / Xác nhận hợp đồng

    /// <summary>
    /// Form ký hợp đồng (Customer)
    /// GET: /Contract/Sign/{id}
    /// </summary>
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Sign(Guid id)
    {
        var model = await _contractService.GetContractForSignAsync(id);
        if (model == null)
        {
            TempData["Error"] = "Không tìm thấy hợp đồng hoặc hợp đồng không ở trạng thái chờ ký.";
            return RedirectToAction(nameof(MyContracts));
        }

        return View(model);
    }

    /// <summary>
    /// Xử lý ký hợp đồng
    /// POST: /Contract/Sign/{id}
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Sign(Guid id, ContractSignViewModel model)
    {
        if (!model.AgreeToTerms)
        {
            ModelState.AddModelError("AgreeToTerms", "Vui lòng đồng ý với điều khoản hợp đồng.");
            var vm = await _contractService.GetContractForSignAsync(id);
            return View(vm);
        }

        try
        {
            var userId = GetCurrentUserId();
            var success = await _contractService.SignContractAsync(id, userId);

            if (!success)
            {
                TempData["Error"] = "Không thể ký hợp đồng.";
                return RedirectToAction(nameof(MyContracts));
            }

            TempData["Success"] = "Đã ký hợp đồng thành công! Vui lòng chờ nhân viên xác nhận.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing contract {ContractId}", id);
            TempData["Error"] = "Có lỗi xảy ra. Vui lòng thử lại.";
            return RedirectToAction(nameof(MyContracts));
        }
    }

    /// <summary>
    /// Form xác nhận hợp đồng (Staff/Admin)
    /// GET: /Contract/Confirm/{id}
    /// </summary>
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Confirm(Guid id)
    {
        var model = await _contractService.GetContractForConfirmAsync(id);
        if (model == null)
        {
            TempData["Error"] = "Không tìm thấy hợp đồng hoặc hợp đồng không thể xác nhận.";
            return RedirectToAction(nameof(Manage));
        }

        return View(model);
    }

    /// <summary>
    /// Xử lý xác nhận hợp đồng
    /// POST: /Contract/Confirm/{id}
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Confirm(Guid id, ContractConfirmViewModel model)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _contractService.ConfirmContractAsync(id, userId, model.ConfirmNote);

            if (!success)
            {
                TempData["Error"] = "Không thể xác nhận hợp đồng.";
                return RedirectToAction(nameof(Manage));
            }

            TempData["Success"] = "Đã xác nhận hợp đồng thành công!";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming contract {ContractId}", id);
            TempData["Error"] = "Có lỗi xảy ra. Vui lòng thử lại.";
            return RedirectToAction(nameof(Manage));
        }
    }

    #endregion

    #region 4.6 Hủy hợp đồng

    /// <summary>
    /// Form hủy hợp đồng
    /// GET: /Contract/Cancel/{id}
    /// </summary>
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var model = await _contractService.GetContractForCancelAsync(id);
        if (model == null)
        {
            TempData["Error"] = "Không tìm thấy hợp đồng hoặc hợp đồng không thể hủy.";
            return RedirectToAction(nameof(Manage));
        }

        return View(model);
    }

    /// <summary>
    /// Xử lý hủy hợp đồng
    /// POST: /Contract/Cancel/{id}
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Cancel(Guid id, ContractCancelViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.CancellationReason))
        {
            ModelState.AddModelError("CancellationReason", "Vui lòng nhập lý do hủy.");
            var vm = await _contractService.GetContractForCancelAsync(id);
            return View(vm);
        }

        try
        {
            var userId = GetCurrentUserId();
            var success = await _contractService.CancelContractAsync(id, userId, model.CancellationReason);

            if (!success)
            {
                TempData["Error"] = "Không thể hủy hợp đồng.";
                return RedirectToAction(nameof(Manage));
            }

            TempData["Success"] = "Đã hủy hợp đồng thành công!";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling contract {ContractId}", id);
            TempData["Error"] = "Có lỗi xảy ra. Vui lòng thử lại.";
            return RedirectToAction(nameof(Manage));
        }
    }

    #endregion

    #region 5.3 Gia hạn

    /// <summary>
    /// Form gia hạn hợp đồng
    /// GET: /Contract/Extend/{id}
    /// </summary>
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Extend(Guid id)
    {
        var model = await _contractService.GetContractForExtendAsync(id);
        if (model == null)
        {
            TempData["Error"] = "Không tìm thấy hợp đồng hoặc hợp đồng không thể gia hạn.";
            return RedirectToAction(nameof(Manage));
        }

        return View(model);
    }

    /// <summary>
    /// Xử lý gia hạn hợp đồng
    /// POST: /Contract/Extend/{id}
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Extend(Guid id, ContractExtendViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var userId = GetCurrentUserId();
            var success = await _contractService.ExtendContractAsync(model, userId);

            if (!success)
            {
                TempData["Error"] = "Không thể gia hạn hợp đồng.";
                return RedirectToAction(nameof(Manage));
            }

            TempData["Success"] = "Đã gia hạn hợp đồng thành công!";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extending contract {ContractId}", id);
            TempData["Error"] = "Có lỗi xảy ra. Vui lòng thử lại.";
            return RedirectToAction(nameof(Manage));
        }
    }

    #endregion

    #region 4.7 In/Xuất hợp đồng

    /// <summary>
    /// Print view hợp đồng
    /// GET: /Contract/Print/{id}
    /// </summary>
    public async Task<IActionResult> Print(Guid id)
    {
        var model = await _contractService.GetContractForPrintAsync(id);
        if (model == null)
        {
            TempData["Error"] = "Không tìm thấy hợp đồng hoặc hợp đồng chưa sẵn sàng in.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    #endregion

    #region API Helpers

    /// <summary>
    /// API: Kiểm tra xe khả dụng
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CheckAvailability(Guid vehicleId, DateTime start, DateTime end)
    {
        var isAvailable = await _contractService.CheckVehicleAvailabilityAsync(vehicleId, start, end);
        return Json(new { isAvailable });
    }

    /// <summary>
    /// API: Tính toán tiền thuê
    /// </summary>
    [HttpGet]
    public IActionResult CalculateAmount(DateTime start, DateTime end, decimal unitPrice, decimal extraCharges = 0)
    {
        var (days, rentalAmount, total) = _contractService.CalculateRentalAmount(start, end, unitPrice, extraCharges);
        return Json(new { days, rentalAmount, total });
    }

    /// <summary>
    /// API: Kiểm tra điều kiện khách hàng
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CheckCustomerEligibility(Guid customerId)
    {
        var (isEligible, reason) = await _contractService.CheckCustomerEligibilityAsync(customerId);
        return Json(new { isEligible, reason });
    }

    #endregion
}
