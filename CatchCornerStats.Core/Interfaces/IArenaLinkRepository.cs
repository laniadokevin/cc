using CatchCornerStats.Core.Entities;

namespace CatchCornerStats.Core.Interfaces
{
    public interface IArenaLinkRepository
    {
        Task<IEnumerable<ArenaLink>> GetAllAsync();
    }
}
