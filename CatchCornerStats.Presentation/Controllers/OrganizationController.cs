using CatchCornerStats.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class OrganizationController : ControllerBase
{
    private readonly IOrganizationRepository _repository;

    public OrganizationController(IOrganizationRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var organizations = await _repository.GetAllAsync();
        return Ok(organizations);
    }
}
