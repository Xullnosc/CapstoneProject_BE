using System.Threading;
using System.Threading.Tasks;
using BusinessObjects.DTOs;

namespace Services
{
    public interface IThesisDuplicationService
    {
        Task<DuplicationCheckResultDTO> CheckAsync(string thesisId, CancellationToken cancellationToken = default);
    }
}
