namespace UCar.Interfaces;

/// <summary>
/// Service upload và quản lý ảnh xe
/// </summary>
public interface IImageUploadService
{
    /// <summary>
    /// Upload nhiều ảnh xe, trả về JSON array của đường dẫn
    /// </summary>
    Task<string> UploadVehicleImagesAsync(Guid contractId, IFormFileCollection files, string imageType);

    /// <summary>
    /// Lấy danh sách ảnh của một hợp đồng
    /// </summary>
    Task<List<string>> GetVehicleImagesAsync(Guid contractId, string imageType);

    /// <summary>
    /// Xóa ảnh
    /// </summary>
    Task<bool> DeleteImageAsync(string imagePath);
}
