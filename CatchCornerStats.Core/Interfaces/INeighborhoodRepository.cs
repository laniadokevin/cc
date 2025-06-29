using CatchCornerStats.Core.Entities;

namespace CatchCornerStats.Core.Interfaces
{
    public interface INeighborhoodRepository
    {
        Task<IEnumerable<Neighborhood>> GetAllAsync();
        Task<Neighborhood?> GetByIdAsync(int id);
    }
}
