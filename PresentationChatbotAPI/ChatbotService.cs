namespace LearningSystemAIAPI;

public class ChatbotService
{
    private readonly MLContext _mlContext;
    private ITransformer _model;
    private PredictionEngine<QuestionAnswerData, QuestionAnswerPrediction> _predictionEngine;

    public ChatbotService()
    {
        _mlContext = new MLContext();
    }

    public void TrainModel(List<QuestionAnswerData> trainingData)
    {
        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

        var pipeline = _mlContext.Transforms.Text.FeaturizeText("Features", nameof(QuestionAnswerData.Question))
            .Append(_mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(QuestionAnswerData.Answer)))
            .Append(_mlContext.MulticlassClassification.Trainers.SdcaNonCalibrated())
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        _model = pipeline.Fit(dataView);
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<QuestionAnswerData, QuestionAnswerPrediction>(_model);
    }

    public string GetAnswer(string question)
    {
        if (_predictionEngine == null)
        {
            return "Model nije obučen.";
        }

        var prediction = _predictionEngine.Predict(new QuestionAnswerData { Question = question });
        return !string.IsNullOrEmpty(prediction.PredictedAnswer)
            ? prediction.PredictedAnswer
            : "Nažalost, ne mogu da pronađem relevantan odgovor.";
    }

    public List<QuestionAnswerData> GenerateTrainingData(string documentText)
    {
        var lines = documentText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                .Where(line => !string.IsNullOrWhiteSpace(line))
                                .Select(line => line.Replace("•", ".").Trim())
                                .ToList();

        var trainingData = new List<QuestionAnswerData>();

        foreach (var line in lines)
        {
            trainingData.Add(new QuestionAnswerData
            {
                Question = $"Šta znači: {line}?",
                Answer = line
            });
        }

        return trainingData;
    }
}

public class QuestionAnswerData
{
    [LoadColumn(0)]
    public string Question { get; set; }
    [LoadColumn(1)]
    public string Answer { get; set; }
}

public class QuestionAnswerPrediction
{
    [ColumnName("PredictedLabel")]
    public string PredictedAnswer { get; set; }
}