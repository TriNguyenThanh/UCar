using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models;
using UCar.Models.DTOs;
using UCar.Models.DTOs.Operations;

namespace UCar.Services;

/// <summary>
/// Service quản lý chi nhánh
/// Tương ứng DFD 8.1 - Quản lý chi nhánh và điểm giao nhận
/// </summary>
public class BranchService : IBranchService
{
    private readonly UCarDbContext _context;

    public BranchService(UCarDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BranchDto>> GetAllAsync()
    {
        return await _context.Branches
            .Select(b => new BranchDto
            {
                BranchId = b.BranchId,
                Name = b.Name,
                Address = b.Address,
                PhoneContact = b.PhoneContact,
                StaffCount = b.StaffProfiles.Count(s => s.IsActive),
                VehicleCount = b.Vehicles.Count(),
                ManagerId = b.StaffProfiles
                    .Where(s => s.IsActive && s.UserAccount != null && s.UserAccount.Role.Code == UCar.Models.Enums.RoleCode.BranchManager)
                    .Select(s => (Guid?)s.StaffId)
                    .FirstOrDefault(),
                ManagerName = b.StaffProfiles
                    .Where(s => s.IsActive && s.UserAccount != null && s.UserAccount.Role.Code == UCar.Models.Enums.RoleCode.BranchManager)
                    .Select(s => s.FullName)
                    .FirstOrDefault()
            })
            .OrderBy(b => b.Name)
            .ToListAsync();
    }

    public async Task<BranchDto?> GetByIdAsync(Guid branchId)
    {
        return await _context.Branches
            .Where(b => b.BranchId == branchId)
            .Select(b => new BranchDto
            {
                BranchId = b.BranchId,
                Name = b.Name,
                Address = b.Address,
                PhoneContact = b.PhoneContact,
                StaffCount = b.StaffProfiles.Count(s => s.IsActive),
                VehicleCount = b.Vehicles.Count(),
                ManagerId = b.StaffProfiles
                    .Where(s => s.IsActive && s.UserAccount != null && s.UserAccount.Role.Code == UCar.Models.Enums.RoleCode.BranchManager)
                    .Select(s => (Guid?)s.StaffId)
                    .FirstOrDefault(),
                ManagerName = b.StaffProfiles
                    .Where(s => s.IsActive && s.UserAccount != null && s.UserAccount.Role.Code == UCar.Models.Enums.RoleCode.BranchManager)
                    .Select(s => s.FullName)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ServiceResult<Guid>> CreateAsync(BranchCreateDto dto)
    {
        // Check duplicate name
        var exists = await _context.Branches.AnyAsync(b => b.Name == dto.Name);
        if (exists)
            return ServiceResult<Guid>.Fail("Tên chi nhánh đã tồn tại");

        var branch = new Branch
        {
            BranchId = Guid.NewGuid(),
            Name = dto.Name,
            Address = dto.Address,
            PhoneContact = dto.PhoneContact
        };

        _context.Branches.Add(branch);
        await _context.SaveChangesAsync();

        return ServiceResult<Guid>.Ok(branch.BranchId, "Tạo chi nhánh thành công");
    }

    public async Task<ServiceResult> UpdateAsync(Guid branchId, BranchUpdateDto dto)
    {
        var branch = await _context.Branches.FindAsync(branchId);
        if (branch == null)
            return ServiceResult.Fail("Không tìm thấy chi nhánh");

        // Check duplicate name (excluding current)
        var exists = await _context.Branches.AnyAsync(b => b.Name == dto.Name && b.BranchId != branchId);
        if (exists)
            return ServiceResult.Fail("Tên chi nhánh đã tồn tại");

        branch.Name = dto.Name;
        branch.Address = dto.Address;
        branch.PhoneContact = dto.PhoneContact;

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Cập nhật chi nhánh thành công");
    }

    public async Task<ServiceResult> DeleteAsync(Guid branchId)
    {
        var branch = await _context.Branches
            .Include(b => b.StaffProfiles)
            .Include(b => b.Vehicles)
            .FirstOrDefaultAsync(b => b.BranchId == branchId);

        if (branch == null)
            return ServiceResult.Fail("Không tìm thấy chi nhánh");

        if (branch.StaffProfiles.Any())
            return ServiceResult.Fail("Không thể xóa chi nhánh đang có nhân viên");

        if (branch.Vehicles.Any())
            return ServiceResult.Fail("Không thể xóa chi nhánh đang có xe");

        _context.Branches.Remove(branch);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Xóa chi nhánh thành công");
    }
}
