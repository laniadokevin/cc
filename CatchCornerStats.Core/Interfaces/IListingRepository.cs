using CatchCornerStats.Core.Entities;

namespace CatchCornerStats.Core.Interfaces
{
    public interface IListingRepository
    {
        Task<IEnumerable<Listing>> GetAllAsync();
        Task<Listing?> GetByFacilityIdAsync(int facilityId);
    }
}
