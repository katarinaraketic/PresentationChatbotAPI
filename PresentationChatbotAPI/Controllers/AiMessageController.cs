namespace LearningSystemAPI.Controllers;

[ApiController, Route("api/[controller]")]
public class AiMessageController : ControllerBase
{
    public class QuoteData
    {
        public string Quote { get; set; }
    }

    public class QuotePrediction
    {
        [ColumnName("PredictedLabel")]
        public uint ClusterId { get; set; }
    }

    private static bool _modelLoaded = false;
    private static Dictionary<uint, List<QuoteData>> _clusters;
    private static readonly object _lock = new object();

    public AiMessageController()
    {
        if (!_modelLoaded)
        {
            lock (_lock)
            {
                if (!_modelLoaded)
                {
                    string csvPath = Path.Combine("Data", "Quotes.csv");
                    var allQuotes = LoadQuotesFromCsv(csvPath);

                    var mlContext = new MLContext();
                    var dataView = mlContext.Data.LoadFromEnumerable(allQuotes);

                    var pipeline = mlContext.Transforms.Text.FeaturizeText(
                                        outputColumnName: "Features",
                                        inputColumnName: nameof(QuoteData.Quote))
                                  .Append(mlContext.Clustering.Trainers.KMeans(
                                        featureColumnName: "Features",
                                        numberOfClusters: 5));

                    var model = pipeline.Fit(dataView);

                    var predEngine = mlContext.Model.CreatePredictionEngine<QuoteData, QuotePrediction>(model);

                    _clusters = new Dictionary<uint, List<QuoteData>>();
                    foreach (var q in allQuotes)
                    {
                        var pred = predEngine.Predict(q);
                        if (!_clusters.ContainsKey(pred.ClusterId))
                            _clusters[pred.ClusterId] = new List<QuoteData>();

                        _clusters[pred.ClusterId].Add(q);
                    }

                    _modelLoaded = true;
                }
            }
        }
    }

    [HttpGet]
    public ActionResult<string> GetAiMessage()
    {
        if (_clusters == null || _clusters.Count == 0)
            return "Nema raspoloživih poruka!";

        var random = new Random();
        var clusterIds = _clusters.Keys.ToList();
        var clusterId = clusterIds[random.Next(clusterIds.Count)];

        var quotesInCluster = _clusters[clusterId];
        var chosenQuote = quotesInCluster[random.Next(quotesInCluster.Count)];

        return Ok(chosenQuote.Quote);
    }

    private List<QuoteData> LoadQuotesFromCsv(string path)
    {
        var result = new List<QuoteData>();
        if (!System.IO.File.Exists(path))
            return result;

        var lines = System.IO.File.ReadAllLines(path);

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (!string.IsNullOrEmpty(line))
            {
                line = line.Trim('"');
                result.Add(new QuoteData { Quote = line });
            }
        }
        return result;
    }
}
