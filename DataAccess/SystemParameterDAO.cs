using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess
{
    public class SystemParameterDAO : ISystemParameterDAO
    {
        private readonly FctmsContext _context;

        public SystemParameterDAO(FctmsContext context)
        {
            _context = context;
        }

        public async Task<List<SystemParameter>> GetAllParametersAsync()
        {
            return await _context.SystemParameters.ToListAsync();
        }

        public async Task<SystemParameter?> GetParameterByKeyAsync(string key)
        {
            return await _context.SystemParameters.FirstOrDefaultAsync(p => p.Key == key);
        }

        public async Task UpdateParameterAsync(SystemParameter parameter)
        {
            parameter.UpdatedAt = DateTime.UtcNow;
            _context.SystemParameters.Update(parameter);
            await _context.SaveChangesAsync();
        }
    }
}
