using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UCar.Interfaces;
using UCar.ViewModels;
using UCar.Models;

namespace UCar.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IAuthService authService, ILogger<AccountController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var user = await _authService.AuthenticateAsync(model.Username, model.Password);
            var fullName = await _authService.GetName(user!);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }

            // Create claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role.Code.ToString())
            };

            if (user.Customer != null)
            {
                claims.Add(new Claim("CustomerId", user.Customer.CustomerId.ToString()));
                claims.Add(new Claim("FullName", fullName));
            }
            else if (user.StaffProfile != null)
            {
                claims.Add(new Claim("StaffId", user.StaffProfile.StaffId.ToString()));
                claims.Add(new Claim("FullName", fullName));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(12)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Update last login time
            await _authService.UpdateLastLoginAsync(user.UserId);

            _logger.LogInformation("User {Username} logged in successfully.", user.Username);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            // Redirect based on role
            return user.Role.Code switch
            {
                Models.Enums.RoleCode.Admin => RedirectToAction("Index", "Home"),
                Models.Enums.RoleCode.Staff => RedirectToAction("Index", "Home"),
                Models.Enums.RoleCode.Customer => RedirectToAction("Index", "Home"),
                _ => RedirectToAction("Index", "Home")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Username}", model.Username);
            ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
            return View(model);
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var username = User.Identity?.Name;
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("User {Username} logged out.", username);
        return RedirectToAction("Login", "Account");
    }

    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return RedirectToAction("Login");
        }

        var user = await _authService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var viewModel = new UserProfileViewModel
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email ?? string.Empty,
            Phone = user.Phone,
            RoleName = user.Role.Name,
            RoleCode = user.Role.Code.ToString(),
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            IsActive = user.IsActive
        };

        if (user.Customer != null)
        {
            viewModel.CustomerId = user.Customer.CustomerId;
            viewModel.CustomerFullName = user.Customer.FullName;
            viewModel.CustomerDob = user.Customer.Dob;
            viewModel.CustomerAddress = user.Customer.AddressText;
            viewModel.RiskLevel = user.Customer.RiskLevel;
            viewModel.IsBlacklisted = user.Customer.IsBlacklisted;
        }
        else if (user.StaffProfile != null)
        {
            viewModel.StaffId = user.StaffProfile.StaffId;
            viewModel.StaffFullName = user.StaffProfile.FullName;
            viewModel.StaffCode = user.StaffProfile.StaffCode;
            viewModel.Position = user.StaffProfile.Position;
            viewModel.BranchName = user.StaffProfile.Branch?.Name;
        }

        return View(viewModel);
    }

    [Authorize]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
