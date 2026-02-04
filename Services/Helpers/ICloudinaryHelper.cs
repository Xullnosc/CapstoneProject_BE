using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Services.Helpers
{
    public interface ICloudinaryHelper
    {
        Task<string> UploadImageAsync(IFormFile file);
    }
}
