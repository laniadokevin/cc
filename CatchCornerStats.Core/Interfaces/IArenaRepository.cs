using CatchCornerStats.Core.Entities;

namespace CatchCornerStats.Core.Interfaces
{
    public interface IArenaRepository
    {
        Task<IEnumerable<Arena>> GetAllAsync();
        Task<Arena?> GetByIdAsync(int facilityId);
    }
}
