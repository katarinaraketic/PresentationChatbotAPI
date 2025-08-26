namespace LearningSystemAPI.Models;

public class SummarizationData
{
    [LoadColumn(0)] public string Sentence { get; set; }
    [LoadColumn(1)] public bool Label { get; set; }
}

public class SummarizationPrediction
{
    [ColumnName("PredictedLabel")] public bool PredictedLabel { get; set; }
    public float Score { get; set; }
}

public class QAData
{
    [LoadColumn(0)] public string Question { get; set; }
    [LoadColumn(1)] public string Sentence { get; set; }
    [LoadColumn(2)] public bool Label { get; set; }
}
public class QAPrediction
{
    [ColumnName("PredictedLabel")] public bool PredictedLabel { get; set; }
    public float Score { get; set; }
}
