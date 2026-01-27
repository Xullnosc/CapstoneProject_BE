using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Services.Helpers
{
    public interface ICloudinaryHelper
    {
        Task<string> UploadImageAsync(IFormFile file);
    }
}
