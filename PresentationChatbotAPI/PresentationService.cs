// File: Services/PresentationService.cs
using System.Text;
using UglyToad.PdfPig;

namespace PresentationChatbotAPI
{
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
    }
}