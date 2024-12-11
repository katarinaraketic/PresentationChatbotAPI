
using Microsoft.ML;
using Microsoft.ML.Data;

namespace PresentationChatbotAPI
{
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



        //public List<QuestionAnswerData> GenerateTrainingData(string documentText)
        //{
        //    var paragraphs = documentText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
        //                                 .Where(p => !string.IsNullOrWhiteSpace(p))
        //                                 .ToList();

        //    var trainingData = new List<QuestionAnswerData>();

        //    foreach (var paragraph in paragraphs)
        //    {
        //        // Pravimo pitanje od naslova slajda
        //        var sentences = paragraph.Split('.');
        //        if (sentences.Length > 1)
        //        {
        //            trainingData.Add(new QuestionAnswerData
        //            {
        //                Question = sentences[0].Trim(), // Naslov ili prva rečenica
        //                Answer = string.Join(". ", sentences.Skip(1)).Trim() // Ostatak kao odgovor
        //            });
        //        }
        //    }

        //    return trainingData;
        //}

        // ova metoda radi
        //public List<QuestionAnswerData> GenerateTrainingData(string documentText)
        //{
        //    var lines = documentText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
        //                            .Where(line => !string.IsNullOrWhiteSpace(line))
        //                            .ToList();

        //    var trainingData = new List<QuestionAnswerData>();

        //    foreach (var line in lines)
        //    {
        //        trainingData.Add(new QuestionAnswerData
        //        {
        //            Question = $"Šta znači: {line.Trim()}?",
        //            Answer = line.Trim()
        //        });
        //    }

        //    return trainingData;
        //}


        // ova metoda radi jos bolje
        //public List<QuestionAnswerData> GenerateTrainingData(string documentText)
        //{
        //    var lines = documentText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
        //                            .Where(line => !string.IsNullOrWhiteSpace(line))
        //                            .Select(line => line.Replace("•", ".").Trim()) // Zamena "•" sa "."
        //                            .ToList();

        //    var trainingData = new List<QuestionAnswerData>();

        //    foreach (var line in lines)
        //    {
        //        trainingData.Add(new QuestionAnswerData
        //        {
        //            Question = $"Šta znači: {line}?",
        //            Answer = line
        //        });
        //    }

        //    return trainingData;
        //}
        public List<QuestionAnswerData> GenerateTrainingData(string documentText)
        {
            var lines = documentText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Where(line => !string.IsNullOrWhiteSpace(line))
                                    .Select(line => line.Replace("•", ".").Trim()) // Zamena "•" sa "."
                                    .Select(line => RemoveUnwantedCharacters(line)) // Uklanja neželjene karaktere
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

        // Metoda za uklanjanje neželjenih brojeva i karaktera
        private string RemoveUnwantedCharacters(string input)
        {
            return input.Trim().TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9').Trim(); // Uklanja sve brojeve na kraju
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
}


//=================================================================
//using Microsoft.ML;
//using Microsoft.ML.Data;

//namespace PresentationChatbotAPI
//{
//    public class ChatbotService
//    {
//        private readonly MLContext _mlContext;
//        private ITransformer _model;
//        private PredictionEngine<QuestionAnswerData, QuestionAnswerPrediction> _predictionEngine;

//        public ChatbotService()
//        {
//            _mlContext = new MLContext();
//        }

//        public void TrainModel(List<QuestionAnswerData> trainingData)
//        {
//            var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

//            var pipeline = _mlContext.Transforms.Text.FeaturizeText("Features", nameof(QuestionAnswerData.Question))
//                .Append(_mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(QuestionAnswerData.Answer)))
//                .Append(_mlContext.MulticlassClassification.Trainers.SdcaNonCalibrated())
//                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

//            _model = pipeline.Fit(dataView);
//            _predictionEngine = _mlContext.Model.CreatePredictionEngine<QuestionAnswerData, QuestionAnswerPrediction>(_model);
//        }

//        public string GetAnswer(string question)
//        {
//            if (_predictionEngine == null)
//            {
//                return "Model nije obučen.";
//            }

//            var prediction = _predictionEngine.Predict(new QuestionAnswerData { Question = question });
//            return !string.IsNullOrEmpty(prediction.PredictedAnswer)
//                ? $"Razumem da pitaš: '{question}'. Evo šta mislim: {prediction.PredictedAnswer}"
//                : "Nažalost, ne mogu da pronađem relevantan odgovor.";
//        }

//        public List<QuestionAnswerData> GenerateTrainingData(string documentText)
//        {
//            var lines = documentText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
//                                    .Where(line => !string.IsNullOrWhiteSpace(line))
//                                    .Select(line => line.Replace("\u2022", ".").Trim()) // Zamena "\u2022" sa "."
//                                    .Select(line => RemoveUnwantedCharacters(line)) // Uklanja neželjene karaktere
//                                    .ToList();

//            var trainingData = new List<QuestionAnswerData>();

//            foreach (var line in lines)
//            {
//                trainingData.Add(new QuestionAnswerData
//                {
//                    Question = $"Šta znači: {line}?",
//                    Answer = line
//                });
//            }

//            return trainingData;
//        }

//        private string RemoveUnwantedCharacters(string input)
//        {
//            return input.Trim().TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9').Trim();
//        }
//    }

//    public class QuestionAnswerData
//    {
//        [LoadColumn(0)]
//        public string Question { get; set; }
//        [LoadColumn(1)]
//        public string Answer { get; set; }
//    }

//    public class QuestionAnswerPrediction
//    {
//        [ColumnName("PredictedLabel")]
//        public string PredictedAnswer { get; set; }
//    }
//}
