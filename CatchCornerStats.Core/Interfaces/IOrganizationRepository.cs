using CatchCornerStats.Core.Entities;

namespace CatchCornerStats.Core.Interfaces
{
    public interface IOrganizationRepository
    {
        Task<IEnumerable<Organization>> GetAllAsync();
    }
}
