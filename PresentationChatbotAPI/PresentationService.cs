namespace PresentationChatbotAPI
{

    using System.Text;
    using UglyToad.PdfPig;

    public class PresentationService
    {


        public string ExtractTextFromPdf(string filePath)
        {
            StringBuilder extractedText = new StringBuilder();

            using (var document = PdfDocument.Open(filePath))
            {
                foreach (var page in document.GetPages())
                {
                    extractedText.AppendLine(page.Text);
                }
            }

            return extractedText.ToString();
        }

        public void GenerateDataset(string extractedText, string outputPath)
        {
            // Podela teksta na paragraf/slajd (ili rečenice)
            var paragraphs = extractedText.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

            // Generisanje pitanja i odgovora
            var dataset = paragraphs.Select((paragraph, index) => new
            {
                Question = $"Šta se nalazi na slajdu {index + 1}?", // Primer pitanja
                Answer = paragraph
            });

            // Snimanje u CSV
            using (var writer = new StreamWriter(outputPath))
            {
                writer.WriteLine("Question,Answer");
                foreach (var data in dataset)
                {
                    writer.WriteLine($"\"{data.Question}\",\"{data.Answer}\"");
                }
            }
        }

    }
}
