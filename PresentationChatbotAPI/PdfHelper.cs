namespace LearningSystemAPI;

public class PdfHelper
{
    public string ExtractTextFromPdf(string filePath)
    {
        var extractedText = new StringBuilder();

        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))

        using (PdfDocument document = PdfDocument.Open(fs))
        {
            foreach (var page in document.GetPages())
            {
                extractedText.AppendLine(page.Text);
            }
        }

        string text = extractedText.ToString();
        // Niz naslova (isti redosled kao u tekstu iznad)
        string[] headers = new[]
        {
    "ITS – Visoka škola strukovnih studija za informacione tehnologije (Beograd)",
    "Studijski programi – osnovne strukovne studije",
    "Master strukovne studije",
    "Akreditacija i kvalitet",
    "Upis, cene i studentski resursi"
};


        foreach (var header in headers)
        {
            text = Regex.Replace(text, @"(?<!\n)" + Regex.Escape(header), "\n" + header);
            text = Regex.Replace(text, Regex.Escape(header) + @"(?!\n)", header + "\n");
        }

        text = Regex.Replace(text, @"(?<=\.)\s+", ".\n");
        text = Regex.Replace(text, @"(\b[A-Z][a-zA-Z\sčćžšđ]+)\s*(\d+)(?=[A-Z])", "$1 $2\n");

        return text;
    }
}
