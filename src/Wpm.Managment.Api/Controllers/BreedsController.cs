using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Wpm.Managment.Api.DataAccess;

namespace Wpm.Managment.Api.Controllers
{
    public class BreedsController(ManagementDbContext managementDbContext, ILogger<BreedsController> logger) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var all = await managementDbContext.Breeds.ToListAsync();
            return all != null ? Ok(all) : NotFound();
        }

        [HttpGet("{id}", Name = nameof(GetBreedsById))]
        public async Task<IActionResult> GetBreedsById(int id)
        {
            var breed = await managementDbContext.Breeds.FindAsync(id);
            return breed != null ? Ok(breed) : NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Create(NewBreed newBreed)
        {
            try
            {
                var breed = newBreed.ToBreed();
                await managementDbContext.Breeds.AddAsync(breed);
                await managementDbContext.SaveChangesAsync();
                return CreatedAtRoute(nameof(GetBreedsById), new { id = breed.Id }, newBreed);
            }
            catch(Exception ex)
            {
                logger.LogError(ex.ToString());
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }

    public record NewBreed(string Name)
    {
        public Breed ToBreed()
        {
            return new Breed(0, Name);
        }
    }
}
