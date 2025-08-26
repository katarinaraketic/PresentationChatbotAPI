namespace LearningSystemAPI.Controllers;

[ApiController, Route("api/[controller]")]
public class SubjectsController(AppDbContext ctx) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SubjectDto>>> GetAll()
    {
        var list = await ctx.Subject
            .Select(s => new SubjectDto { Id = s.Id, Name = s.Name, Description = s.Description })
            .ToListAsync();
        return Ok(list);
    }
}

