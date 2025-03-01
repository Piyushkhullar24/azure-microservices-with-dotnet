using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wpm.Managment.Api.DataAccess;

namespace Wpm.Managment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PetsController(ManagementDbContext dbContext) : ControllerBase
{
    [HttpGet]
   public async Task<IActionResult> Get()
    {
       return Ok(await dbContext.Pets.Include(p=>p.Breed).ToListAsync());
    }

    [HttpGet("{id}", Name = nameof(GetById))]
    public async Task<IActionResult> GetById(int id)
    {
        return Ok(await dbContext.Pets.Include(p => p.Breed).Where(p => p.Id == id).FirstOrDefaultAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create(NewPet newPet)
    {
        var pet = newPet.ToPet();
        await dbContext.Pets.AddAsync(pet);
        await dbContext.SaveChangesAsync();
        return CreatedAtRoute(nameof(GetById), new { id = pet.Id }, newPet);

    }
}


public record NewPet(string Name, int Age, int BreedId)
{
    public Pet ToPet()
    {
        return new Pet
        {
            Name = Name,
            Age = Age,
            BreedId = BreedId
        };
    }
}