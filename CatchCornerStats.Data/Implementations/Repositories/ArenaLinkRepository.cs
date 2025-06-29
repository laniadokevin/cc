using CatchCornerStats.Core.Interfaces;
using CatchCornerStats.Core.Entities;
using CatchCornerStats.Data;
using Microsoft.EntityFrameworkCore;

namespace CatchCornerStats.Data.Repositories
{
    public class ArenaLinkRepository : IArenaLinkRepository
    {
        private readonly AppDbContext _context;

        public ArenaLinkRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ArenaLink>> GetAllAsync()
        {
            return await _context.ArenaLinks.ToListAsync();
        }
    }
}
