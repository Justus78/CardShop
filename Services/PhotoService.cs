using api.Exceptions;
using api.Helpers;
using api.Interfaces;
using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Microsoft.Extensions.Options;

namespace api.Services
{
    public class PhotoService : IPhotoService
    {
        // Upload limits. Adjust MaxFileSizeBytes if your card photos are larger.
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };

        private readonly Cloudinary _cloudinary;
        private readonly ILogger<PhotoService> _logger;

        public PhotoService(IOptions<CloudinarySettings> config, ILogger<PhotoService> logger)
        {
            var acc = new Account(
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
                );

            _cloudinary = new Cloudinary(acc);
            _logger = logger;
        }

        public async Task<ImageUploadResult> AddPhotoAsync(IFormFile file)
        {
            // Everything the client controls (name, content type, size, bytes) is checked
            // BEFORE anything is sent to Cloudinary. BusinessRuleException becomes a 400
            // through the global exception middleware.
            await ValidateImageAsync(file);

            using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Transformation = new Transformation().Height(500).Crop("fill").Gravity("face")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            // Cloudinary reports failures INSIDE the result instead of throwing. Without
            // this check, a bad API key in production would silently produce a result
            // with no URL and nothing in the logs.
            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary upload failed: {Error}", uploadResult.Error.Message);
            }
            else
            {
                _logger.LogInformation("Uploaded image {PublicId} ({SizeKb} KB).",
                    uploadResult.PublicId, file.Length / 1024);
            }

            return uploadResult;
        }

        public async Task<DeletionResult> DeletePhotoAsync(string publicId)
        {
            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);

            if (result.Error != null)
            {
                _logger.LogError("Cloudinary delete failed for {PublicId}: {Error}", publicId, result.Error.Message);
            }
            else if (result.Result != "ok")
            {
                // e.g. "not found": the image was already gone. Not fatal, but worth knowing.
                _logger.LogWarning("Cloudinary delete for {PublicId} returned '{Result}'.", publicId, result.Result);
            }
            else
            {
                _logger.LogInformation("Deleted image {PublicId}.", publicId);
            }

            return result;
        }

        private static async Task ValidateImageAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                throw new BusinessRuleException("No image file was provided.");

            if (file.Length > MaxFileSizeBytes)
                throw new BusinessRuleException(
                    $"Image is too large. The maximum size is {MaxFileSizeBytes / (1024 * 1024)} MB.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                throw new BusinessRuleException("Only JPG, PNG and WebP images are allowed.");

            if (!AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
                throw new BusinessRuleException("Only JPG, PNG and WebP images are allowed.");

            // The extension and content type are just text the client typed, so also check
            // that the file's first bytes really are a JPEG, PNG or WebP image.
            var header = new byte[12];
            await using (var stream = file.OpenReadStream())
            {
                var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
                if (read < header.Length || !HasImageSignature(header))
                    throw new BusinessRuleException("The file is not a valid JPG, PNG or WebP image.");
            }
        }

        private static bool HasImageSignature(byte[] h)
        {
            // JPEG: FF D8 FF
            if (h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return true;

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47 &&
                h[4] == 0x0D && h[5] == 0x0A && h[6] == 0x1A && h[7] == 0x0A) return true;

            // WebP: "RIFF" + 4 size bytes + "WEBP"
            if (h[0] == 'R' && h[1] == 'I' && h[2] == 'F' && h[3] == 'F' &&
                h[8] == 'W' && h[9] == 'E' && h[10] == 'B' && h[11] == 'P') return true;

            return false;
        }
    }
}