using CatchCornerStats.Core.Interfaces;
using CatchCornerStats.Core.Entities;
using CatchCornerStats.Data;
using Microsoft.EntityFrameworkCore;

namespace CatchCornerStats.Data.Repositories
{
    public class ArenaRepository : IArenaRepository
    {
        private readonly AppDbContext _context;

        public ArenaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Arena>> GetAllAsync()
        {
            return await _context.Arenas.ToListAsync();
        }

        public async Task<Arena?> GetByIdAsync(int facilityId)
        {
            return await _context.Arenas.FirstOrDefaultAsync(a => a.FacilityId == facilityId);
        }
    }
}
