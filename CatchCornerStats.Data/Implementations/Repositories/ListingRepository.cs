using CatchCornerStats.Core.Interfaces;
using CatchCornerStats.Core.Entities;
using CatchCornerStats.Data;
using Microsoft.EntityFrameworkCore;

namespace CatchCornerStats.Data.Repositories
{
    public class ListingRepository : IListingRepository
    {
        private readonly AppDbContext _context;

        public ListingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Listing>> GetAllAsync()
        {
            return await _context.Listings.ToListAsync();
        }

        public async Task<Listing?> GetByFacilityIdAsync(int facilityId)
        {
            return await _context.Listings.FirstOrDefaultAsync(l => l.FacilityId == facilityId);
        }
    }
}
