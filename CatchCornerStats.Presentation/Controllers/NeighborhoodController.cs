using CatchCornerStats.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class NeighborhoodController : ControllerBase
{
    private readonly INeighborhoodRepository _repository;

    public NeighborhoodController(INeighborhoodRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var neighborhoods = await _repository.GetAllAsync();
        return Ok(neighborhoods);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var neighborhood = await _repository.GetByIdAsync(id);
        if (neighborhood == null) return NotFound();

        return Ok(neighborhood);
    }
}
