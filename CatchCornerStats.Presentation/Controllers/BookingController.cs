using CatchCornerStats.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CatchCornerStats.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly IBookingRepository _repository;

        public BookingController(IBookingRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var bookings = await _repository.GetAllAsync();
            return Ok(bookings);
        }

        [HttpGet("{number}")]
        public async Task<IActionResult> GetByNumber(int number)
        {
            var booking = await _repository.GetByNumberAsync(number);
            if (booking == null) return NotFound();

            return Ok(booking);
        }
    }
}
