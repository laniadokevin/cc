using CatchCornerStats.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CatchCornerStats.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArenaLinkController : ControllerBase
    {
        private readonly IArenaLinkRepository _repository;

        public ArenaLinkController(IArenaLinkRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var links = await _repository.GetAllAsync();
            return Ok(links);
        }
    }
}
