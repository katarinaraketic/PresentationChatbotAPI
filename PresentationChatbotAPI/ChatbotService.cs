namespace LearningSystemAPI;
using System.Text.RegularExpressions;


public class ChatbotService
{
    private readonly MLContext _mlContext;
    private ITransformer _model;
    private PredictionEngine<QuestionCategory, QuestionPrediction> _predEngine;
    private Dictionary<string, string> _categoryToAnswer;

    private const int HeaderMaxLen = 80;   // ranije 60 – malo opuštenije
    private const int MinContentLen = 60;  // ranije 80 – tolerantnije


    public ChatbotService()
    {
        _mlContext = new MLContext();
        _categoryToAnswer = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public void TrainModelFromDocument(string documentText)
    {
        if (string.IsNullOrWhiteSpace(documentText))
            throw new ArgumentException("Tekst dokumenta ne može biti prazan.", nameof(documentText));

        Console.WriteLine("\n========== Ekstrahovani tekst iz dokumenta ==========\n");
        Console.WriteLine(documentText);
        Console.WriteLine("\n======================================================\n");

        var categories = ExtractCategories(documentText);

        Console.WriteLine($"\n✅ Pronađeno kategorija: {categories.Count}");
        foreach (var (cat, content) in categories)
        {
            Console.WriteLine($"🟢 Kategorija: {cat}");
            Console.WriteLine($"🔹 Prvih 100 karaktera sadržaja: {content.Substring(0, Math.Min(100, content.Length))}...\n");
        }

        if (categories == null || !categories.Any())
            throw new InvalidOperationException("Nije pronađena nijedna validna kategorija u dokumentu.");

        var trainingData = new List<QuestionCategory>();
        var templates = new[]
        {
            "Šta je {0}?",
            "Objasni {0}.",
            "Kako funkcioniše {0}?",
            "Definiši {0}.",
            "Objasni mi {0}.",
            "Koji je značaj {0}?",
            "Zašto je važan {0}?"
        };

        foreach (var (category, content) in categories)
        {
            _categoryToAnswer[category] = content;
            foreach (var template in templates)
            {
                trainingData.Add(new QuestionCategory
                {
                    QuestionText = string.Format(template, category),
                    Label = category
                });
            }
        }

        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);
        var pipeline = _mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: "LabelKey", inputColumnName: "Label")
            .Append(_mlContext.Transforms.Text.FeaturizeText(outputColumnName: "Features", inputColumnName: nameof(QuestionCategory.QuestionText)))
            .AppendCacheCheckpoint(_mlContext)
            .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(labelColumnName: "LabelKey", featureColumnName: "Features"))
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue(outputColumnName: "PredictedLabel", inputColumnName: "PredictedLabel"));

        _model = pipeline.Fit(dataView);
        _predEngine = _mlContext.Model.CreatePredictionEngine<QuestionCategory, QuestionPrediction>(_model);
    }

    private static string Normalize(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        // unificiraj kraj linije
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");

        // ukloni NBSP i soft-hyphen
        text = text.Replace('\u00A0', ' ');
        text = text.Replace("\u00AD", ""); // soft hyphen

        // spoji reči razlomljene na kraju reda: npr. "doku-\nment"
        text = Regex.Replace(text, @"(?<=\w)-\s*\n(?=\w)", "");

        // ukloni duple/tab razmake (ali NE diramo \n)
        text = Regex.Replace(text, @"[ \t]+", " ");

        return text;
    }


    public string GetAnswer(string question)
    {
        if (_predEngine == null)
            return "Model nije obučen.";
        if (string.IsNullOrWhiteSpace(question))
            return "Pitanje ne može biti prazno.";

        var prediction = _predEngine.Predict(new QuestionCategory { QuestionText = question });
        return _categoryToAnswer.TryGetValue(prediction.PredictedCategory, out var answer)
            ? answer
            : "Nažalost, ne mogu da pronađem relevantan odgovor.";
    }

    //public List<(string Category, string Content)> ExtractCategories(string documentText)
    //{
    //    var result = new List<(string, string)>();
    //    var seenTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    //    var lines = documentText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
    //                            .Select(l => l.Trim())
    //                            .Where(l => !string.IsNullOrWhiteSpace(l))
    //                            .ToList();

    //    for (int i = 0; i < lines.Count; i++)
    //    {
    //        var line = lines[i];
    //        Console.WriteLine($"[DEBUG] i={i}, line='{line}' (len={line.Length})");

    //        if (IsPotentialHeader(line, seenTitles))
    //        {
    //            string header = line;
    //            Console.WriteLine($"[DEBUG]  -> HEADER prepoznat: '{header}'");

    //            var contentBuilder = new StringBuilder();
    //            int j = i + 1;

    //            while (j < lines.Count && !IsPotentialHeader(lines[j], seenTitles))
    //            {
    //                var candidate = lines[j];
    //                Console.WriteLine($"[DEBUG]    -> Sadržaj linija j={j}, '{candidate}' (len={candidate.Length})");

    //                if (candidate.StartsWith(header, StringComparison.OrdinalIgnoreCase))
    //                {
    //                    candidate = candidate.Substring(header.Length).Trim();
    //                    Console.WriteLine($"[DEBUG]    -> Posle uklanjanja headera: '{candidate}'");
    //                }

    //                if (!string.IsNullOrEmpty(candidate))
    //                    contentBuilder.AppendLine(candidate);

    //                j++;
    //            }

    //            var finalText = contentBuilder.ToString().Trim();
    //            Console.WriteLine($"[DEBUG]  -> finalText dužina = {finalText.Length}");

    //            if (finalText.Length > 80)
    //            {
    //                Console.WriteLine($"✅ Dodajemo u kategorije: '{header}'");
    //                result.Add((header, finalText));
    //                seenTitles.Add(header);
    //            }
    //            else
    //            {
    //                Console.WriteLine($"⚠️  Odbacujemo '{header}' jer ima premalo teksta.");
    //            }

    //            i = j - 1;
    //        }
    //    }

    //    return result;
    //}

    public List<(string Category, string Content)> ExtractCategories(string documentText)
    {
        var result = new List<(string, string)>();
        var seenTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1) Normalizuj sirovi tekst
        var normalized = Normalize(documentText);
        if (string.IsNullOrWhiteSpace(normalized))
            return result;

        // 2) Napravi linije
        var lines = normalized
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            Console.WriteLine($"[DEBUG] i={i}, line='{line}' (len={line.Length})");

            if (IsPotentialHeader(lines, i, seenTitles))
            {
                string header = lines[i];
                Console.WriteLine($"[DEBUG]  -> HEADER prepoznat: '{header}'");

                var contentBuilder = new StringBuilder();
                int j = i + 1;

                while (j < lines.Count && !IsPotentialHeader(lines, j, seenTitles))
                {
                    var candidate = lines[j];

                    // Ako ekstraktor ponavlja header na početku sledeće linije – skini ga
                    if (candidate.StartsWith(header, StringComparison.OrdinalIgnoreCase))
                    {
                        candidate = candidate.Substring(header.Length).Trim();
                        Console.WriteLine($"[DEBUG]    -> Posle uklanjanja headera: '{candidate}'");
                    }

                    if (!string.IsNullOrEmpty(candidate))
                        contentBuilder.AppendLine(candidate);

                    j++;
                }

                var finalText = contentBuilder.ToString().Trim();
                Console.WriteLine($"[DEBUG]  -> finalText dužina = {finalText.Length}");

                if (finalText.Length >= MinContentLen)
                {
                    Console.WriteLine($"✅ Dodajemo u kategorije: '{header}'");
                    result.Add((header, finalText));
                    seenTitles.Add(header);
                }
                else
                {
                    Console.WriteLine($"⚠️  Odbacujemo '{header}' jer ima premalo teksta.");
                }

                i = j - 1; // preskoči već potrošene linije
            }
        }

        // Fallback – ako PDF/ekstrakcija nisu “čisti”, ne padamo na exception
        if (result.Count == 0 && !string.IsNullOrWhiteSpace(normalized))
        {
            var fallback = normalized.Length > 4000 ? normalized.Substring(0, 4000) : normalized;
            result.Add(("Opšte informacije", fallback));
            Console.WriteLine("⚠️  Fallback kategorija dodata: 'Opšte informacije'");
        }

        return result;
    }

    //private bool IsPotentialHeader(string line, HashSet<string> seenTitles)
    //{
    //    if (line.Length > 60) return false;
    //    if (!char.IsUpper(line[0])) return false;
    //    if (line.EndsWith(":") || line.EndsWith("?")) return false;
    //    if (seenTitles.Contains(line)) return false;

    //    return true;
    //}
    private bool IsPotentialHeader(IReadOnlyList<string> lines, int i, HashSet<string> seenTitles)
    {
        if (i < 0 || i >= lines.Count) return false;

        var line = lines[i]?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(line)) return false;

        // 1) osnovne provere
        if (line.Length > HeaderMaxLen) return false;
        if (!char.IsLetter(line[0]) || !char.IsUpper(line[0])) return false;

        // ne dozvoli da liči na običnu rečenicu
        if (line.EndsWith(":") || line.EndsWith("?") || line.EndsWith(".")) return false;
        if (Regex.IsMatch(line, @"[.!?]")) return false; // bilo koji interpunkcijski znak u sredini

        // broj reči 1..10 (naslovi su kratki)
        var wordCount = line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount < 1 || wordCount > 10) return false;

        // izbegni brojeve/stranice i sl.
        if (Regex.IsMatch(line, @"^\d+(\.\d+)*$")) return false;

        if (seenTitles.Contains(line)) return false;

        // 2) pogled unapred – sledeća ne-prazna linija mora ličiti na pasus
        var next = NextNonEmpty(lines, i + 1);
        if (string.IsNullOrEmpty(next)) return false;

        // Ako je sledeća linija vrlo kratka i ne završava se tačkom – verovatno nije pasus
        var nextCondensed = Regex.Replace(next, @"\s+", " ");
        if (nextCondensed.Length < 40 && !nextCondensed.EndsWith(".")) return false;

        return true;
    }

    private static string? NextNonEmpty(IReadOnlyList<string> lines, int start)
    {
        for (int k = start; k < lines.Count; k++)
        {
            var s = lines[k]?.Trim();
            if (!string.IsNullOrEmpty(s)) return s;
        }
        return null;
    }

}

public class QuestionCategory
{
    public string QuestionText { get; set; }
    public string Label { get; set; }
}

public class QuestionPrediction
{
    [ColumnName("PredictedLabel")]
    public string PredictedCategory { get; set; }
}
