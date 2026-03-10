using BusinessObjects.DTOs;
using BusinessObjects.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public interface IThesisService
    {
        // Existing (do not modify)
        Task<Thesis> ProposeThesisAsync(ProposeThesisDTO req, string email);
        Task<Thesis?> GetThesisByIdAsync(string id);
        Task<IEnumerable<Thesis>> GetAllThesesAsync();
        Task<IEnumerable<Thesis>> GetThesesByUserIdAsync(int userId);
        Task UpdateThesisStatusAsync(string thesisId, string status);

        // Phase 02: New methods
        Task<ThesisDTO> UpdateThesisAsync(string thesisId, UpdateThesisDTO req, string email);
        Task<ThesisDTO> CancelThesisAsync(string thesisId, string email);
        Task<IEnumerable<ThesisDTO>> GetMyThesesAsync(string email, string? status = null, string? searchTitle = null);
        Task<ThesisDTO?> GetThesisDetailAsync(string id);
        Task<IEnumerable<ThesisDTO>> GetFilteredThesesAsync(string? status, int? userId, string? searchTitle = null, int? semesterId = null);
    }
}
