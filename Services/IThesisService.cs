using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public interface IThesisService
    {
        Task<Thesis> ProposeThesisAsync(ProposeThesisDTO req, string email);
        Task<Thesis?> GetThesisByIdAsync(string id);
        Task<IEnumerable<Thesis>> GetAllThesesAsync();
        Task<IEnumerable<Thesis>> GetThesesByUserIdAsync(int userId);
        Task UpdateThesisStatusAsync(string thesisId, string status);
    }
}
