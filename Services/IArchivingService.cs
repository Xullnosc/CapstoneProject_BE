using System.Threading.Tasks;

namespace Services
{
    public interface IArchivingService
    {
        Task ArchiveSemesterAsync(int semesterId);
    }
}
