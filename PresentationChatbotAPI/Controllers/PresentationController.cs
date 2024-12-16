namespace PresentationChatbotAPI.Controllers;

[ApiController, Route("api/[controller]")]
public class PresentationController(PresentationService presentationService, ChatbotService chatbotService) : ControllerBase
{
    [HttpPost("process-all")]
    public async Task<IActionResult> ProcessAndTrainModel([FromForm] Kaca kaca, [FromQuery] string fileUrl)
    {
        if ((kaca.Kacica == null || kaca.Kacica.Length == 0) && string.IsNullOrEmpty(fileUrl))
        {
            return BadRequest("Morate dostaviti fajl ili URL.");
        }

        var combinedTextTasks = new List<Task<string>>();

        if (kaca.Kacica != null && kaca.Kacica.Length > 0)
        {
            combinedTextTasks.Add(Task.Run(async () =>
            {
                var tempFilePath = Path.GetTempFileName();
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await kaca.Kacica.CopyToAsync(stream);
                }
                return presentationService.ExtractTextFromPdf(tempFilePath);
            }));
        }

        if (!string.IsNullOrEmpty(fileUrl))
        {
            combinedTextTasks.Add(Task.Run(async () =>
            {
                var tempFilePath = Path.GetTempFileName();
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(fileUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new FileNotFoundException("Prezentacija sa dostavljenog URL-a nije pronađena.");
                    }

                    using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                }
                return presentationService.ExtractTextFromPdf(tempFilePath);
            }));
        }

        var combinedTexts = await Task.WhenAll(combinedTextTasks);
        var combinedText = string.Join("\n", combinedTexts);

        var trainingData = chatbotService.GenerateTrainingData(combinedText);
        chatbotService.TrainModel(trainingData);

        return Ok(new { Message = "Tekst iz fajla i/ili URL-a je uspešno obrađen i model je obučen." });
    }

    [HttpGet("ask")]
    public IActionResult AskQuestion([FromQuery] string question)
    {
        if (string.IsNullOrEmpty(question))
        {
            return BadRequest("Pitanje nije dostavljeno.");
        }

        var answer = chatbotService.GetAnswer(question);
        return Ok(new { Answer = answer });
    }
}
