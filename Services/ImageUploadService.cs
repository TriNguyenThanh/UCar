using System.Text.Json;
using UCar.Interfaces;

namespace UCar.Services;

/// <summary>
/// Service upload và quản lý ảnh xe
/// Lưu vào thư mục wwwroot/uploads/vehicles/{contractId}/{imageType}/
/// </summary>
public class ImageUploadService : IImageUploadService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ImageUploadService> _logger;
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
    private readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    public ImageUploadService(IWebHostEnvironment environment, ILogger<ImageUploadService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<string> UploadVehicleImagesAsync(Guid contractId, IFormFileCollection files, string imageType)
    {
        if (files == null || files.Count == 0)
            return "[]";

        var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "vehicles", contractId.ToString(), imageType);
        
        if (!Directory.Exists(uploadPath))
            Directory.CreateDirectory(uploadPath);

        var uploadedPaths = new List<string>();

        foreach (var file in files)
        {
            if (file.Length == 0) continue;
            if (file.Length > MaxFileSize)
            {
                _logger.LogWarning("File {FileName} exceeds max size {MaxSize}", file.FileName, MaxFileSize);
                continue;
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                _logger.LogWarning("File {FileName} has invalid extension {Extension}", file.FileName, extension);
                continue;
            }

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadPath, fileName);

            try
            {
                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                // Trả về đường dẫn tương đối từ wwwroot
                var relativePath = $"/uploads/vehicles/{contractId}/{imageType}/{fileName}";
                uploadedPaths.Add(relativePath);

                _logger.LogInformation("Uploaded image: {Path}", relativePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file {FileName}", file.FileName);
            }
        }

        return JsonSerializer.Serialize(uploadedPaths);
    }

    public Task<List<string>> GetVehicleImagesAsync(Guid contractId, string imageType)
    {
        var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "vehicles", contractId.ToString(), imageType);
        
        if (!Directory.Exists(uploadPath))
            return Task.FromResult(new List<string>());

        var files = Directory.GetFiles(uploadPath)
            .Select(f => $"/uploads/vehicles/{contractId}/{imageType}/{Path.GetFileName(f)}")
            .ToList();

        return Task.FromResult(files);
    }

    public Task<bool> DeleteImageAsync(string imagePath)
    {
        try
        {
            // imagePath: /uploads/vehicles/{contractId}/{imageType}/{fileName}
            var fullPath = Path.Combine(_environment.WebRootPath, imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Deleted image: {Path}", imagePath);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete image {Path}", imagePath);
            return Task.FromResult(false);
        }
    }
}
