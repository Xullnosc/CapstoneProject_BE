using Repositories;
using System.Threading.Tasks;

namespace Services
{
    public class ArchivingService : IArchivingService
    {
        private readonly IArchivingRepository _archivingRepository;

        public ArchivingService(IArchivingRepository archivingRepository)
        {
            _archivingRepository = archivingRepository;
        }

        public async Task ArchiveSemesterAsync(int semesterId)
        {
            await _archivingRepository.ArchiveSemesterAsync(semesterId);
        }
    }
}
