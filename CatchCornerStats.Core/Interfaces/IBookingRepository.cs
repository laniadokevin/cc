using CatchCornerStats.Core.Entities;

namespace CatchCornerStats.Core.Interfaces
{
    public interface IBookingRepository
    {
        Task<IEnumerable<Booking>> GetAllAsync();
        Task<Booking?> GetByNumberAsync(int bookingNumber);
    }
}
