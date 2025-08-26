namespace LearningSystemAPI.Models
{
    public class QuizRequest
    {
        public int PresIndex { get; set; }
    }

    public class QuizQuestion
    {
        public int SlideNumber { get; set; }
        public string Question { get; set; }
        public List<string> Options { get; set; }
        public int CorrectOptionIndex { get; set; }
    }

    public class QuizAnswerRequest
    {
        public int PresIndex { get; set; }
        public List<int> SelectedOptionIndices { get; set; }
    }

    public class QuizResult
    {
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public double ScorePercent => (double)CorrectAnswers / TotalQuestions * 100;
    }
}
