using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Shared.EWS.Data;
using Shared.EWS.Exceptions;
using Shared.EWS.Interfaces.Services;

namespace Shared.EWS.Services
{
    public class FileService : IFileService
    {
        private readonly string _baseAttachmentPath;
        private readonly EWSDbContext _context;
        private const long MaxAttachmentSize = 10 * 1024 * 1024; // 10 MB

        private static readonly string[] AllowedAttachmentTypes =
            [".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx", ".txt"];

        public string BaseAttachmentPath => _baseAttachmentPath;

        public FileService(IWebHostEnvironment env)
        {
            _baseAttachmentPath = Path.Combine(
                Directory.GetParent(env.ContentRootPath)!.FullName,
                "Shared.EWS", "Resources", "Attachments"
            );
        }

        public async Task<string> SaveAttachmentAsync(IFormFile file, string subFolder)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("File is empty or null.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!AllowedAttachmentTypes.Contains(extension))
                throw new InvalidOperationException($"Extension {extension} not allowed.");

            if (file.Length > MaxAttachmentSize)
                throw new InvalidOperationException("File size exceeds limit.");


            var folderPath = Path.Combine(_baseAttachmentPath, subFolder);
            Directory.CreateDirectory(folderPath);

            var diskName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(folderPath, diskName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return diskName;
        }

        public async Task<FileContentHttpResult> GetFileResultAsync(string fileName, string subFolder, string? originalName = null)
        {
            var filePath = Path.Combine(_baseAttachmentPath, subFolder, fileName);

            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found on server.");

            var bytes = await File.ReadAllBytesAsync(filePath);
            var contentType = ResolveContentType(fileName);

            return TypedResults.File(bytes, contentType, originalName ?? fileName);
        }

        public async Task DeleteAttachmentAsync(string fileName, string subFolder)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;

            var attachment = await _context.TaskAttachments
                .FirstOrDefaultAsync(a => a.FileName == fileName && !a.IsDeleted);

            if (attachment == null)
                throw new NotFoundException($"Attachment with name '{fileName}' was not found.");

            attachment.IsDeleted = true;
            attachment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var filePath = Path.Combine(_baseAttachmentPath, subFolder, fileName);
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        private static string ResolveContentType(string fileName) =>
            Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };
    }
}