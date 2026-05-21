using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Shared.EWS.Interfaces.Services
{
    public interface IFileService
    {
        Task<string> SaveAttachmentAsync(IFormFile file, string subFolder);

        Task<FileContentHttpResult> GetFileResultAsync(string fileName, string subFolder, string? originalName = null);
        
        Task DeleteAttachmentAsync(string fileName, string subFolder);
        
        string BaseAttachmentPath { get; }
    }
}