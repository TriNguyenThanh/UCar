using System.Security.Claims;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models.Enums;

namespace UCar.Services;

/// <summary>
/// Service xác định quyền truy cập theo chi nhánh
/// </summary>
public class BranchAccessService : IBranchAccessService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UCarDbContext _context;

    // Custom claim types for branch access
    public const string BranchIdClaimType = "BranchId";
    public const string RoleCodeClaimType = "RoleCode";

    public BranchAccessService(IHttpContextAccessor httpContextAccessor, UCarDbContext context)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
    }

    public Guid? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier);
        
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            return userId;
        
        return null;
    }

    public RoleCode? GetCurrentUserRole()
    {
        var roleClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst(RoleCodeClaimType);
        
        if (roleClaim != null && Enum.TryParse<RoleCode>(roleClaim.Value, out var roleCode))
            return roleCode;
        
        // Fallback: check role claim
        var roleNameClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.Role);
        
        if (roleNameClaim != null && Enum.TryParse<RoleCode>(roleNameClaim.Value, out var roleFromName))
            return roleFromName;
        
        return null;
    }

    public Guid? GetCurrentUserBranchId()
    {
        // Admin có quyền truy cập tất cả chi nhánh
        if (IsAdmin())
            return null;
        
        // Lấy BranchId từ claim
        var branchIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst(BranchIdClaimType);
        
        if (branchIdClaim != null && Guid.TryParse(branchIdClaim.Value, out var branchId))
            return branchId;
        
        return null;
    }

    public bool IsAdmin()
    {
        var role = GetCurrentUserRole();
        return role == RoleCode.Admin;
    }

    public bool IsBranchManager()
    {
        var role = GetCurrentUserRole();
        return role == RoleCode.BranchManager;
    }

    public bool CanAccessBranch(Guid branchId)
    {
        // Admin có thể truy cập mọi chi nhánh
        if (IsAdmin())
            return true;
        
        // BranchManager và Staff chỉ được truy cập chi nhánh của mình
        var userBranchId = GetCurrentUserBranchId();
        return userBranchId.HasValue && userBranchId.Value == branchId;
    }
}
