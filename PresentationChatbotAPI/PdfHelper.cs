namespace LearningSystemAIAPI;

public class PdfHelper
{
    public string ExtractTextFromPdf(string filePath)
    {
        StringBuilder extractedText = new();

        using (PdfDocument? document = PdfDocument.Open(filePath))
        {
            foreach (var page in document.GetPages())
            {
                extractedText.AppendLine(page.Text);
            }
        }

        return extractedText.ToString();
    }
}