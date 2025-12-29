using Microsoft.AspNetCore.Mvc;
using UCar.Interfaces;
using UCar.Models.DTOs.Handover;

namespace UCar.Controllers;

/// <summary>
/// Controller quản lý giao nhận xe
/// Tương ứng DFD 6.0 - QUẢN LÝ GIAO NHẬN XE
/// </summary>
public class HandoverController : Controller
{
    private readonly IHandoverService _handoverService;

    public HandoverController(IHandoverService handoverService)
    {
        _handoverService = handoverService;
    }

    /// <summary>
    /// Danh sách hợp đồng chờ giao xe (Check-out list)
    /// GET: /Handover
    /// </summary>
    public async Task<IActionResult> Index(HandoverFilterDto filter)
    {
        var result = await _handoverService.GetContractsForHandoverAsync(filter);
        ViewBag.Filter = filter;
        return View(result);
    }

    /// <summary>
    /// Danh sách hợp đồng chờ nhận xe (Check-in list)
    /// GET: /Handover/Return
    /// </summary>
    public async Task<IActionResult> Return(HandoverFilterDto filter)
    {
        var result = await _handoverService.GetContractsForReturnAsync(filter);
        ViewBag.Filter = filter;
        return View(result);
    }

    /// <summary>
    /// Danh sách hợp đồng quá hạn
    /// GET: /Handover/Overdue
    /// </summary>
    public async Task<IActionResult> Overdue()
    {
        var result = await _handoverService.GetOverdueContractsAsync();
        return View(result);
    }

    /// <summary>
    /// Form giao xe (Check-out)
    /// GET: /Handover/CheckOut/{contractId}
    /// </summary>
    public async Task<IActionResult> CheckOut(Guid contractId)
    {
        var form = await _handoverService.GetCheckOutFormAsync(contractId);
        if (form == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hợp đồng";
            return RedirectToAction(nameof(Index));
        }

        var canCheckOut = await _handoverService.CanCheckOutAsync(contractId);
        if (!canCheckOut.Success)
        {
            TempData["ErrorMessage"] = canCheckOut.Errors.First();
            return RedirectToAction(nameof(Index));
        }

        return View(form);
    }

    /// <summary>
    /// Xác nhận giao xe
    /// POST: /Handover/CheckOut
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckOut(CheckOutDto dto)
    {
        if (!ModelState.IsValid)
        {
            var form = await _handoverService.GetCheckOutFormAsync(dto.ContractId);
            return View(form);
        }

        // TODO: Lấy userId từ session/authentication
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var result = await _handoverService.ConfirmCheckOutAsync(dto, userId);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Errors.First();
            var form = await _handoverService.GetCheckOutFormAsync(dto.ContractId);
            return View(form);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(HandoverDocument), new { contractId = dto.ContractId });
    }

    /// <summary>
    /// Form nhận xe (Check-in)
    /// GET: /Handover/CheckIn/{contractId}
    /// </summary>
    public async Task<IActionResult> CheckIn(Guid contractId)
    {
        var form = await _handoverService.GetCheckInFormAsync(contractId);
        if (form == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hợp đồng hoặc chưa giao xe";
            return RedirectToAction(nameof(Return));
        }

        var canCheckIn = await _handoverService.CanCheckInAsync(contractId);
        if (!canCheckIn.Success)
        {
            TempData["ErrorMessage"] = canCheckIn.Errors.First();
            return RedirectToAction(nameof(Return));
        }

        return View(form);
    }

    /// <summary>
    /// Xác nhận nhận xe
    /// POST: /Handover/CheckIn
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(CheckInDto dto)
    {
        if (!ModelState.IsValid)
        {
            var form = await _handoverService.GetCheckInFormAsync(dto.ContractId);
            return View(form);
        }

        // TODO: Lấy userId từ session/authentication
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var result = await _handoverService.ConfirmCheckInAsync(dto, userId);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Errors.First();
            var form = await _handoverService.GetCheckInFormAsync(dto.ContractId);
            return View(form);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(ReturnDocument), new { contractId = dto.ContractId });
    }

    /// <summary>
    /// Chi tiết biên bản giao xe
    /// GET: /Handover/HandoverDocument/{contractId}
    /// </summary>
    public async Task<IActionResult> HandoverDocument(Guid contractId)
    {
        var record = await _handoverService.GetHandoverRecordAsync(contractId);
        if (record == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy biên bản giao xe";
            return RedirectToAction(nameof(Index));
        }
        return View(record);
    }

    /// <summary>
    /// Chi tiết biên bản nhận xe
    /// GET: /Handover/ReturnDocument/{contractId}
    /// </summary>
    public async Task<IActionResult> ReturnDocument(Guid contractId)
    {
        var record = await _handoverService.GetReturnRecordAsync(contractId);
        if (record == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy biên bản nhận xe";
            return RedirectToAction(nameof(Return));
        }
        return View(record);
    }

    /// <summary>
    /// Danh sách tất cả biên bản giao/nhận
    /// GET: /Handover/Documents
    /// </summary>
    public async Task<IActionResult> Documents(HandoverDocumentFilterDto filter)
    {
        var result = await _handoverService.GetDocumentsAsync(filter);
        ViewBag.Filter = filter;
        return View(result);
    }

    #region Incidents (D13)

    /// <summary>
    /// Danh sách sự cố của hợp đồng
    /// GET: /Handover/Incidents/{contractId}
    /// </summary>
    public async Task<IActionResult> Incidents(Guid contractId)
    {
        var incidents = await _handoverService.GetIncidentsByContractAsync(contractId);
        ViewBag.ContractId = contractId;
        return View(incidents);
    }

    /// <summary>
    /// Form tạo sự cố mới
    /// GET: /Handover/CreateIncident/{contractId}
    /// </summary>
    public IActionResult CreateIncident(Guid contractId)
    {
        var dto = new IncidentCreateDto { ContractId = contractId, OccurredAt = DateTime.Now };
        return View(dto);
    }

    /// <summary>
    /// Submit tạo sự cố
    /// POST: /Handover/CreateIncident
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateIncident(IncidentCreateDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var result = await _handoverService.CreateIncidentAsync(dto, userId);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Errors.First();
            return View(dto);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Incidents), new { contractId = dto.ContractId });
    }

    #endregion

    #region Violations (D13)

    /// <summary>
    /// Danh sách vi phạm của hợp đồng
    /// GET: /Handover/Violations/{contractId}
    /// </summary>
    public async Task<IActionResult> Violations(Guid contractId)
    {
        var violations = await _handoverService.GetViolationsByContractAsync(contractId);
        ViewBag.ContractId = contractId;
        return View(violations);
    }

    /// <summary>
    /// Form tạo vi phạm mới
    /// GET: /Handover/CreateViolation/{contractId}
    /// </summary>
    public IActionResult CreateViolation(Guid contractId)
    {
        var dto = new ViolationCreateDto { ContractId = contractId };
        return View(dto);
    }

    /// <summary>
    /// Submit tạo vi phạm
    /// POST: /Handover/CreateViolation
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateViolation(ViolationCreateDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var result = await _handoverService.CreateViolationAsync(dto, userId);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Errors.First();
            return View(dto);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Violations), new { contractId = dto.ContractId });
    }

    #endregion
}
