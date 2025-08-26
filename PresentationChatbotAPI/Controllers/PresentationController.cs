namespace LearningSystemAPI.Controllers;

[ApiController, Route("api/[controller]")]
public class PresentationController(PdfHelper presentationService, ChatbotService chatbotService, MLService mlService, AppDbContext appDbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<PresentationDto>>> GetAll()
    {
        var list = await appDbContext.Presentations
            .Include(p => p.Subject)
            .Select(p => new PresentationDto
            {
                Id = p.Id,
                Label = p.Label,
                FileName = p.FileName,
                ContentType = p.ContentType,
                SubjectId = p.Subject.Id,
                SubjectName = p.Subject.Name
            })
            .ToListAsync();
        return Ok(list);
    }


    [HttpGet("{id:guid}/file")]
    public async Task<IActionResult> GetFile(Guid id)
    {
        var pres = await appDbContext.Presentations
            .Where(p => p.Id == id)
            .Select(p => new { p.FileData, p.ContentType, p.FileName })
            .FirstOrDefaultAsync();

        if (pres == null)
            return NotFound();

        return File(pres.FileData, pres.ContentType, pres.FileName);
    }

    [HttpPost]
    public async Task<ActionResult> Upload([FromForm] UploadPresentationDto dto)
    {

        using var ms = new MemoryStream();
        await dto.File.OpenReadStream().CopyToAsync(ms);

        var ext = Path.GetExtension(dto.File.FileName);

        var pres = new Presentations
        {
            Id = Guid.NewGuid(),
            Label = "",
            FileName = dto.File.FileName  ,
            ContentType = dto.File.ContentType ?? "application/octet-stream",
            FileData = ms.ToArray(),
            Summary = ""
        };

        appDbContext.Presentations.Add(pres);
        await appDbContext.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetFile),
            new { id = pres.Id },
            new { pres.Id }
        );
    }

    [HttpPost("process-all")]
    public async Task<IActionResult> ProcessAndTrainModel([FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            return BadRequest("Morate dostaviti bar jedan fajl.");

        var allTexts = new List<string>();

        foreach (var file in files)
        {
            if (file.Length == 0)
                continue;

            string tmpPath = Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}"
            );

            using (var writeStream = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(writeStream);
            }

            string extracted = presentationService.ExtractTextFromPdf(tmpPath);
            allTexts.Add(extracted);

            System.IO.File.Delete(tmpPath);
        }

        chatbotService.TrainModelFromDocument(string.Join("\n", allTexts));

        return Ok(new { Message = "Tekst iz svih fajlova je obrađen i model je obučen." });
    }

    [HttpGet("ask")]
    public IActionResult AskQuestion([FromQuery] string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return BadRequest("Pitanje nije dostavljeno.");

        var answer = chatbotService.GetAnswer(question);
        return Ok(new { Answer = answer });
    }

    [HttpGet("summarize")]
    public IActionResult Summarize([FromQuery] int index)
    {
        var text = LoadPresentationText(index);
        var summary = mlService.Summarize(text);
        return Ok(new { Text = summary });
    }

    [HttpPost("askAdvanced")]
    public IActionResult AskAdvanced([FromBody] AskRequest req)
    {
        var context = LoadPresentationText(req.Index);
        var answer = mlService.Answer(context, req.Question);
        return Ok(new { Answer = answer });
    }

    private string LoadPresentationText(int index)
    {
        var path = Path.Combine("Data", $"Presentation{index + 1}.txt");
        return System.IO.File.Exists(path)
            ? System.IO.File.ReadAllText(path)
            : string.Empty;
    }

    public class AskRequest
    {
        public int Index { get; set; }
        public string Question { get; set; }
    }

    [HttpPost("bulk-upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult> BulkUpload([FromForm] IFormFile[] files)
    {
        if (files == null || files.Length == 0)
            return BadRequest("Morate uploadovati bar jedan fajl.");

        var uploadedIds = new List<Guid>();
        foreach (var file in files)
        {
            if (file.Length == 0) continue;
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            Presentations pres = new Presentations
            {
                Id = Guid.NewGuid(),
                Label = Path.GetFileNameWithoutExtension(file.FileName),
                FileName = file.FileName,
                ContentType = file.ContentType ?? "application/octet-stream",
                FileData = ms.ToArray(),
                Summary = ""
            };
            appDbContext.Presentations.Add(pres);
            uploadedIds.Add(pres.Id);
        }

        await appDbContext.SaveChangesAsync();
        return Ok(new { Count = uploadedIds.Count, Ids = uploadedIds });
    }
}
