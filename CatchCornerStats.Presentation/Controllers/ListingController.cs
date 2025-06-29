using CatchCornerStats.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CatchCornerStats.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ListingController : ControllerBase
    {
        private readonly IListingRepository _repository;

        public ListingController(IListingRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var listings = await _repository.GetAllAsync();
            return Ok(listings);
        }

        [HttpGet("{facilityId}")]
        public async Task<IActionResult> GetByFacilityId(int facilityId)
        {
            var listing = await _repository.GetByFacilityIdAsync(facilityId);
            if (listing == null) return NotFound();

            return Ok(listing);
        }
    }
}
