namespace LearningSystemAPI.Controllers;

[ApiController, Route("api/learning/quiz")]
public class QuizController(IWebHostEnvironment env) : ControllerBase
{
    private static readonly MLContext _mlContext = new MLContext(seed: 0);

    [HttpPost]
    public ActionResult<List<QuizQuestion>> GenerateQuiz([FromBody] QuizRequest req)
    {
        var slideTexts = LoadSlideTexts(req.PresIndex);
        int maxQ = 5;
        int count = Math.Min(maxQ, slideTexts.Count);
        if (count == 0)
            return Ok(new List<QuizQuestion> {
                new QuizQuestion {
                    SlideNumber = 1,
                    Question = "Nema dovoljno teksta za pitanje",
                    Options = new List<string>{ "N/A" },
                    CorrectOptionIndex = 0
                }
            });

        var data = slideTexts
            .Take(count)
            .Select(t => new TextInput { Text = t })
            .ToList();
        IDataView dv = _mlContext.Data.LoadFromEnumerable(data);

        var pipeline = _mlContext.Transforms.Text
            .ProduceWordBags(
                outputColumnName: "Features",
                inputColumnName: nameof(TextInput.Text),
                ngramLength: 1,
                useAllLengths: false);

        var transformer = pipeline.Fit(dv);
        var transformed = transformer.Transform(dv);

        VBuffer<ReadOnlyMemory<char>> slotNames = default;
        transformed.Schema["Features"]
            .Annotations
            .GetValue("SlotNames", ref slotNames);
        var vocab = slotNames
            .DenseValues()
            .Select(x => x.ToString())
            .ToArray();

        var featureVectors = _mlContext.Data
            .CreateEnumerable<FeatureRow>(transformed, reuseRowObject: false)
            .Select(fr => fr.Features)
            .ToList();

        var questions = new List<QuizQuestion>();
        for (int i = 0; i < count; i++)
        {
            var fv = featureVectors[i];
            var topIdx = fv
                .Select((v, idx) => (v, idx))
                .OrderByDescending(t => t.v)
                .Take(3)
                .Select(t => t.idx)
                .ToList();

            var topTerms = topIdx
                .Select(idx => vocab[idx])
                .ToList();

            var opts = topTerms
                .OrderBy(_ => Guid.NewGuid())
                .ToList();
            opts.Add("Nepovezan odgovor");
            opts = opts.OrderBy(_ => Guid.NewGuid()).ToList();

            questions.Add(new QuizQuestion
            {
                SlideNumber = i + 1,
                Question = $"Koji pojam najčešće opisuje slajd {i + 1}?",
                Options = opts,
                CorrectOptionIndex = opts.IndexOf(topTerms[0])
            });
        }

        return Ok(questions);
    }

    [HttpPost("submit")]
    public ActionResult<QuizResult> SubmitQuiz([FromBody] QuizAnswerRequest ans)
    {
        var quiz = GenerateQuiz(new QuizRequest
        {
            PresIndex = ans.PresIndex
        }).Value
        ?? new List<QuizQuestion>();

        int correct = quiz
            .Select((q, i) => ans.SelectedOptionIndices.ElementAtOrDefault(i) == q.CorrectOptionIndex)
            .Count(isRight => isRight);

        return Ok(new QuizResult
        {
            TotalQuestions = quiz.Count,
            CorrectAnswers = correct
        });
    }


    private List<string> LoadSlideTexts(int presIndex)
    {
        var root = env.ContentRootPath;
        var path = Path.Combine(root, "Presentations", "sample_quiz.pdf");
        if (!System.IO.File.Exists(path)) return new List<string>();

        var pages = new List<string>();
        using var pdf = PdfDocument.Open(path);
        foreach (var page in pdf.GetPages())
        {
            var txt = page.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(txt))
                pages.Add(txt);
        }
        return pages;
    }

    private class TextInput
    {
        public string Text { get; set; } = "";
    }

    private class FeatureRow
    {
        [VectorType]
        public float[] Features { get; set; } = null!;
    }
}
