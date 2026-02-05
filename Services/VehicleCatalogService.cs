using Microsoft.EntityFrameworkCore;
using UCar.Data;
using UCar.Interfaces;
using UCar.Models;
using UCar.Models.DTOs;
using UCar.Models.DTOs.Vehicle;

namespace UCar.Services;

/// <summary>
/// Service quản lý danh mục xe (loại xe, dòng xe)
/// </summary>
public class VehicleCatalogService : IVehicleCatalogService
{
    private readonly UCarDbContext _context;

    public VehicleCatalogService(UCarDbContext context)
    {
        _context = context;
    }

    #region Vehicle Types

    public async Task<IEnumerable<VehicleTypeDto>> GetAllVehicleTypesAsync()
    {
        return await _context.VehicleTypes
            .OrderBy(vt => vt.TypeName)
            .Select(vt => new VehicleTypeDto(
                vt.VehicleTypeId,
                vt.TypeName,
                null, // Description - need to add to model if required
                vt.VehicleModels.Count()
            ))
            .ToListAsync();
    }

    public async Task<VehicleTypeDto?> GetVehicleTypeByIdAsync(Guid id)
    {
        return await _context.VehicleTypes
            .Where(vt => vt.VehicleTypeId == id)
            .Select(vt => new VehicleTypeDto(
                vt.VehicleTypeId,
                vt.TypeName,
                null,
                vt.VehicleModels.Count()
            ))
            .FirstOrDefaultAsync();
    }

    public async Task<ServiceResult<Guid>> CreateVehicleTypeAsync(VehicleTypeCreateDto dto)
    {
        // Check for duplicate name
        var exists = await _context.VehicleTypes
            .AnyAsync(vt => vt.TypeName.ToLower() == dto.TypeName.ToLower().Trim());

        if (exists)
        {
            return ServiceResult<Guid>.Fail("Loại xe đã tồn tại");
        }

        var vehicleType = new VehicleType
        {
            VehicleTypeId = Guid.NewGuid(),
            TypeName = dto.TypeName.Trim()
        };

        _context.VehicleTypes.Add(vehicleType);
        await _context.SaveChangesAsync();

        return ServiceResult<Guid>.Ok(vehicleType.VehicleTypeId, "Thêm loại xe thành công");
    }

    public async Task<ServiceResult> UpdateVehicleTypeAsync(Guid id, VehicleTypeUpdateDto dto)
    {
        var vehicleType = await _context.VehicleTypes.FindAsync(id);
        if (vehicleType == null)
        {
            return ServiceResult.Fail("Loại xe không tồn tại");
        }

        // Check for duplicate name (exclude current)
        var exists = await _context.VehicleTypes
            .AnyAsync(vt => vt.VehicleTypeId != id && 
                           vt.TypeName.ToLower() == dto.TypeName.ToLower().Trim());

        if (exists)
        {
            return ServiceResult.Fail("Tên loại xe đã tồn tại");
        }

        vehicleType.TypeName = dto.TypeName.Trim();
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Cập nhật loại xe thành công");
    }

    public async Task<ServiceResult> DeleteVehicleTypeAsync(Guid id)
    {
        var vehicleType = await _context.VehicleTypes
            .Include(vt => vt.VehicleModels)
            .FirstOrDefaultAsync(vt => vt.VehicleTypeId == id);

        if (vehicleType == null)
        {
            return ServiceResult.Fail("Loại xe không tồn tại");
        }

        if (vehicleType.VehicleModels.Any())
        {
            return ServiceResult.Fail("Không thể xóa loại xe đã có dòng xe liên kết");
        }

        _context.VehicleTypes.Remove(vehicleType);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Xóa loại xe thành công");
    }

    #endregion

    #region Vehicle Models

    public async Task<IEnumerable<VehicleModelDto>> GetAllVehicleModelsAsync(Guid? vehicleTypeId = null)
    {
        var query = _context.VehicleModels
            .AsQueryable();

        if (vehicleTypeId.HasValue)
        {
            query = query.Where(vm => vm.VehicleTypeId == vehicleTypeId.Value);
        }

        return await query
            .OrderBy(vm => vm.Make)
            .ThenBy(vm => vm.ModelName)
            .Select(vm => new VehicleModelDto(
                vm.ModelId,
                vm.VehicleTypeId,
                vm.VehicleType.TypeName,
                vm.Make,
                vm.ModelName,
                vm.Seats,
                vm.Transmission,
                vm.FuelType,
                vm.ImageFileName,
                vm.Vehicles.Count()
            ))
            .ToListAsync();
    }

    public async Task<VehicleModelDto?> GetVehicleModelByIdAsync(Guid id)
    {
        return await _context.VehicleModels
            .Where(vm => vm.ModelId == id)
            .Select(vm => new VehicleModelDto(
                vm.ModelId,
                vm.VehicleTypeId,
                vm.VehicleType.TypeName,
                vm.Make,
                vm.ModelName,
                vm.Seats,
                vm.Transmission,
                vm.FuelType,
                vm.ImageFileName,
                vm.Vehicles.Count()
            ))
            .FirstOrDefaultAsync();
    }

    public async Task<ServiceResult<Guid>> CreateVehicleModelAsync(VehicleModelCreateDto dto)
    {
        // Validate VehicleTypeId exists
        var typeExists = await _context.VehicleTypes.AnyAsync(vt => vt.VehicleTypeId == dto.VehicleTypeId);
        if (!typeExists)
        {
            return ServiceResult<Guid>.Fail("Loại xe không tồn tại");
        }

        // Check for duplicate Make + ModelName
        var exists = await _context.VehicleModels
            .AnyAsync(vm => vm.Make.ToLower() == dto.Make.ToLower().Trim() &&
                           vm.ModelName.ToLower() == dto.ModelName.ToLower().Trim());

        if (exists)
        {
            return ServiceResult<Guid>.Fail("Dòng xe đã tồn tại");
        }

        var vehicleModel = new VehicleModel
        {
            ModelId = Guid.NewGuid(),
            VehicleTypeId = dto.VehicleTypeId,
            Make = dto.Make.Trim(),
            ModelName = dto.ModelName.Trim(),
            Seats = dto.Seats,
            Transmission = dto.Transmission,
            FuelType = dto.FuelType?.Trim(),
            ImageFileName = dto.ImageFileName
        };

        _context.VehicleModels.Add(vehicleModel);
        await _context.SaveChangesAsync();

        return ServiceResult<Guid>.Ok(vehicleModel.ModelId, "Thêm dòng xe thành công");
    }

    public async Task<ServiceResult> UpdateVehicleModelAsync(Guid id, VehicleModelUpdateDto dto)
    {
        var vehicleModel = await _context.VehicleModels.FindAsync(id);
        if (vehicleModel == null)
        {
            return ServiceResult.Fail("Dòng xe không tồn tại");
        }

        // Validate VehicleTypeId exists
        var typeExists = await _context.VehicleTypes.AnyAsync(vt => vt.VehicleTypeId == dto.VehicleTypeId);
        if (!typeExists)
        {
            return ServiceResult.Fail("Loại xe không tồn tại");
        }

        // Check for duplicate Make + ModelName (exclude current)
        var exists = await _context.VehicleModels
            .AnyAsync(vm => vm.ModelId != id &&
                           vm.Make.ToLower() == dto.Make.ToLower().Trim() &&
                           vm.ModelName.ToLower() == dto.ModelName.ToLower().Trim());

        if (exists)
        {
            return ServiceResult.Fail("Dòng xe đã tồn tại");
        }

        vehicleModel.VehicleTypeId = dto.VehicleTypeId;
        vehicleModel.Make = dto.Make.Trim();
        vehicleModel.ModelName = dto.ModelName.Trim();
        vehicleModel.Seats = dto.Seats;
        vehicleModel.Transmission = dto.Transmission;
        vehicleModel.FuelType = dto.FuelType?.Trim();
        
        // Update image only if provided
        if (!string.IsNullOrEmpty(dto.ImageFileName))
        {
            vehicleModel.ImageFileName = dto.ImageFileName;
        }

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Cập nhật dòng xe thành công");
    }

    public async Task<ServiceResult> DeleteVehicleModelAsync(Guid id)
    {
        var vehicleModel = await _context.VehicleModels
            .Include(vm => vm.Vehicles)
            .FirstOrDefaultAsync(vm => vm.ModelId == id);

        if (vehicleModel == null)
        {
            return ServiceResult.Fail("Dòng xe không tồn tại");
        }

        if (vehicleModel.Vehicles.Any())
        {
            return ServiceResult.Fail("Không thể xóa dòng xe đã có xe liên kết");
        }

        _context.VehicleModels.Remove(vehicleModel);
        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Xóa dòng xe thành công");
    }

    #endregion
}
