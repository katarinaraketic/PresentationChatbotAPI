// Ako ti ipak treba OpenXML negde drugde, ališuj ga ovako:
using OoxmlPresentation = DocumentFormat.OpenXml.Presentation.Presentation;

// Alias za EF entitet iz DatabaseFirst namespace-a:
using DbPres = LearningSystemAPI.DatabaseFirst.Presentations;

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace LearningSystemAPI.Controllers;

[ApiController, Route("api/[controller]")]
public class PresentationController(PdfHelper presentationService, ChatbotService chatbotService, AppDbContext appDbContext) : ControllerBase
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

    // ---- BULK UPLOAD ----
    public sealed class BulkPresentationUploadForm
    {
        [Required]
        public Guid SubjectId { get; set; }

        /// <summary>
        /// Opcioni label za sve fajlove; ako nije zadat, koristi se naziv fajla bez ekstenzije.
        /// </summary>
        public string? Label { get; set; }

        [Required]
        public List<IFormFile> Files { get; set; } = new();
    }

    public sealed class BulkUploadResult
    {
        public int Inserted { get; set; }
        public int Skipped { get; set; }
        public List<Guid> CreatedIds { get; set; } = new();
        public List<string> Messages { get; set; } = new();
    }

    [HttpPost("bulk")]
    [Consumes("multipart/form-data")]
    [DisableRequestSizeLimit] // dozvoli veće PDF-ove
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<ActionResult<BulkUploadResult>> BulkUpload(
        [FromForm] BulkPresentationUploadForm form,
        [FromQuery] bool skipDuplicates = true,           // ako postoji fajl sa istim imenom za isti subject, preskače se
        CancellationToken ct = default)
    {
        if (form.Files == null || form.Files.Count == 0)
            return BadRequest("Nema poslatih fajlova.");

        var toInsert = new List<Presentations>(form.Files.Count);
        var result = new BulkUploadResult();

        // Opciona transakcija – ili sve ili ništa
        await using var trx = await appDbContext.Database.BeginTransactionAsync(ct);

        foreach (var file in form.Files)
        {
            if (file.Length <= 0)
            {
                result.Skipped++;
                result.Messages.Add($"{file.FileName}: prazan fajl.");
                continue;
            }

            // Duplikati po (SubjectId, FileName)
            if (skipDuplicates)
            {
                var exists = await appDbContext.Presentations
                    .AnyAsync(p => p.SubjectId == form.SubjectId && p.FileName == file.FileName, ct);

                if (exists)
                {
                    result.Skipped++;
                    result.Messages.Add($"{file.FileName}: već postoji, preskačem.");
                    continue;
                }
            }

            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);

            var entity = new Presentations
            {
                Id = Guid.NewGuid(),
                Label = string.IsNullOrWhiteSpace(form.Label)
                                ? Path.GetFileNameWithoutExtension(file.FileName)
                                : form.Label,
                Summary = null,
                FileData = ms.ToArray(),
                FileName = file.FileName,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                                ? "application/octet-stream"
                                : file.ContentType,
                SubjectId = form.SubjectId
            };

            toInsert.Add(entity);
        }

        if (toInsert.Count == 0)
        {
            await trx.RollbackAsync(ct);
            return Ok(result); // sve je preskočeno
        }

        await appDbContext.Presentations.AddRangeAsync(toInsert, ct);
        await appDbContext.SaveChangesAsync(ct);
        await trx.CommitAsync(ct);

        result.Inserted = toInsert.Count;
        result.CreatedIds = toInsert.Select(e => e.Id).ToList();

        return CreatedAtAction(nameof(GetAll), null, result);
    }
}
