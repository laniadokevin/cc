using CatchCornerStats.Core.Interfaces;
using CatchCornerStats.Core.Entities;
using CatchCornerStats.Data;
using Microsoft.EntityFrameworkCore;

namespace CatchCornerStats.Data.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly AppDbContext _context;

        public BookingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Booking>> GetAllAsync()
        {
            return await _context.Bookings.ToListAsync();
        }

        public async Task<Booking?> GetByNumberAsync(int bookingNumber)
        {
            return await _context.Bookings.FirstOrDefaultAsync(b => b.BookingNumber == bookingNumber);
        }
    }
}
