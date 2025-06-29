using CatchCornerStats.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ArenaController : ControllerBase
{
    private readonly IArenaRepository _arenaRepository;

    public ArenaController(IArenaRepository arenaRepository)
    {
        _arenaRepository = arenaRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var arenas = await _arenaRepository.GetAllAsync();
        return Ok(arenas);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var arena = await _arenaRepository.GetByIdAsync(id);
        if (arena == null) return NotFound();

        return Ok(arena);
    }
}
