using CatchCornerStats.Core.Interfaces;
using CatchCornerStats.Core.Entities;
using CatchCornerStats.Data;
using Microsoft.EntityFrameworkCore;

namespace CatchCornerStats.Data.Repositories
{
    public class NeighborhoodRepository : INeighborhoodRepository
    {
        private readonly AppDbContext _context;

        public NeighborhoodRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Neighborhood>> GetAllAsync()
        {
            return await _context.Neighborhoods.ToListAsync();
        }

        public async Task<Neighborhood?> GetByIdAsync(int id)
        {
            return await _context.Neighborhoods.FirstOrDefaultAsync(n => n.Id == id);
        }
    }
}
