namespace LearningSystemAPI.Controllers;

[ApiController, Route("api/[controller]")]
public class DbController (AppDbContext appDbContext): ControllerBase
{
    [HttpGet("Subjects")]
    public async Task<List<Subject>> Subjects()
    { 
        return await appDbContext.Subject.ToListAsync();
    }
}
