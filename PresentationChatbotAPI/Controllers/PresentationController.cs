using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace PresentationChatbotAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PresentationController : ControllerBase
    {
        private readonly PresentationService _presentationService;
        private readonly ChatbotService _chatbotService;

        public PresentationController(PresentationService presentationService, ChatbotService chatbotService)
        {
            _presentationService = presentationService;
            _chatbotService = chatbotService;
        }

        [HttpPost("process-predefined")]
        public async Task<IActionResult> ProcessPredefinedPresentation()
        {
            var fileUrl = "http://localhost:4200/BRM24_01_M.pdf";
            var tempFilePath = Path.GetTempFileName();

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(fileUrl);
                if (!response.IsSuccessStatusCode)
                {
                    return NotFound("Prezentacija nije pronađena.");
                }

                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                {
                    await response.Content.CopyToAsync(fileStream);
                }
            }

            var extractedText = _presentationService.ExtractTextFromPdf(tempFilePath);

            var trainingData = _chatbotService.GenerateTrainingData(extractedText);

            _chatbotService.TrainModel(trainingData);

            return Ok(new { Message = "Tekst iz PDF-a je uspešno učitan i model je obučen." });
        }

        [HttpGet("ask")]
        public IActionResult AskQuestion([FromQuery] string question)
        {
            if (string.IsNullOrEmpty(question))
            {
                return BadRequest("Pitanje nije dostavljeno.");
            }

            var answer = _chatbotService.GetAnswer(question);
            return Ok(new { Answer = answer });
        }



        [HttpPost("process-book")]
        public async Task<IActionResult> ProcessBook([FromForm] Kaca kaca)
        {
            if (kaca.Kacica == null || kaca.Kacica.Length == 0)
            {
                return BadRequest("Knjiga nije dostavljena ili je prazna.");
            }

            var tempFilePath = Path.GetTempFileName();

            // Sačuvaj knjigu privremeno
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await kaca.Kacica.CopyToAsync(stream);
            }

            // Ekstrakcija teksta iz PDF knjige
            var extractedText = _presentationService.ExtractTextFromPdf(tempFilePath);

            // Generisanje trening podataka iz teksta knjige
            var trainingData = _chatbotService.GenerateTrainingData(extractedText);

            // Obučavanje modela sa generisanim podacima
            _chatbotService.TrainModel(trainingData);

            return Ok(new { Message = "Tekst iz knjige je uspešno učitan i model je obučen." });
        }

    }
}