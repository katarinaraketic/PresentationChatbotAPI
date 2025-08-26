namespace LearningSystemAPI;

public class MLService
{
    private readonly string _summarizerModelPath = Path.Combine("Models", "SummarizerModel.zip");
    private readonly string _qaModelPath = Path.Combine("Models", "QAModel.zip");
    private readonly MLContext _mlContext;
    private PredictionEngine<SummarizationData, SummarizationPrediction> _summarizerEngine;
    private PredictionEngine<QAData, QAPrediction> _qaEngine;

    public MLService()
    {
        _mlContext = new MLContext(seed: 0);

        if (!File.Exists(_summarizerModelPath)) TrainSummarizer();
        if (!File.Exists(_qaModelPath)) TrainQA();

        LoadModels();
    }

    private void TrainSummarizer()
    {
        var dataPath = Path.Combine("MLData", "summarization_data.csv");
        var dataView = _mlContext.Data.LoadFromTextFile<SummarizationData>(
            path: dataPath, hasHeader: true, separatorChar: ',');

        var pipeline = _mlContext.Transforms.Text
                .FeaturizeText(outputColumnName: "Features", inputColumnName: nameof(SummarizationData.Sentence))
            .Append(_mlContext.BinaryClassification
                .Trainers.SdcaLogisticRegression(
                    labelColumnName: nameof(SummarizationData.Label),
                    featureColumnName: "Features"));

        var model = pipeline.Fit(dataView);
        Directory.CreateDirectory("Models");
        _mlContext.Model.Save(model, dataView.Schema, _summarizerModelPath);
    }

    private void TrainQA()
    {
        var dataPath = Path.Combine("MLData", "qa_data.csv");
        var dataView = _mlContext.Data.LoadFromTextFile<QAData>(
            path: dataPath, hasHeader: true, separatorChar: ',');

        var pipeline = _mlContext.Transforms.Concatenate("Features",
                nameof(QAData.Question), nameof(QAData.Sentence))
            .Append(_mlContext.Transforms.Text
                .FeaturizeText(outputColumnName: "Features"))
            .Append(_mlContext.BinaryClassification
                .Trainers.SdcaLogisticRegression(
                    labelColumnName: nameof(QAData.Label),
                    featureColumnName: "Features"));

        var model = pipeline.Fit(dataView);
        Directory.CreateDirectory("Models");
        _mlContext.Model.Save(model, dataView.Schema, _qaModelPath);
    }

    private void LoadModels()
    {
        ITransformer summModel = _mlContext.Model.Load(_summarizerModelPath, out _);
        _summarizerEngine = _mlContext.Model
            .CreatePredictionEngine<SummarizationData, SummarizationPrediction>(summModel);

        ITransformer qaModel = _mlContext.Model.Load(_qaModelPath, out _);
        _qaEngine = _mlContext.Model
            .CreatePredictionEngine<QAData, QAPrediction>(qaModel);
    }

    public string Summarize(string text, int maxSentences = 3)
    {
        var sentences = text
            .Split(new[] { ". ", "? ", "! " }, StringSplitOptions.RemoveEmptyEntries);

        var scored = sentences
            .Select(s => new
            {
                Sentence = s,
                Score = _summarizerEngine
                              .Predict(new SummarizationData { Sentence = s })
                              .Score
            })
            .OrderByDescending(x => x.Score)
            .Take(maxSentences)
            .Select(x => x.Sentence.Trim());

        return string.Join(". ", scored) + ".";
    }

    public string Answer(string context, string question)
    {
        var sentences = context
            .Split(new[] { ". ", "? ", "! " }, StringSplitOptions.RemoveEmptyEntries);

        var best = sentences
            .Select(s => new
            {
                Sentence = s,
                Score = _qaEngine
                              .Predict(new QAData { Question = question, Sentence = s })
                              .Score
            })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        return best?.Sentence.Trim()
               ?? "Нема довољно информација за одговор.";
    }
}

