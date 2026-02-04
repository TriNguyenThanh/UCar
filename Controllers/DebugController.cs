using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace UCar.Controllers;

[Authorize]
public class DebugController : Controller
{
    [HttpGet]
    public IActionResult Claims()
    {
        ViewBag.IsAuthenticated = User.Identity?.IsAuthenticated ?? false;
        ViewBag.AuthenticationType = User.Identity?.AuthenticationType;
        ViewBag.Name = User.Identity?.Name;
        ViewBag.IsInCustomerRole = User.IsInRole("Customer");
        ViewBag.IsInStaffRole = User.IsInRole("Staff");
        ViewBag.Claims = User.Claims.ToList();
        
        return View();
    }
}
