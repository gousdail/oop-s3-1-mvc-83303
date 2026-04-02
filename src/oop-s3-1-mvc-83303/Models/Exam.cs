namespace oop_s3_1_mvc_83303.Models
{
    public class Exam
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public virtual Course? Course { get; set; }

        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public double MaxScore { get; set; }
        public bool ResultsReleased { get; set; }

        public virtual ICollection<ExamResult> Results { get; set; } = new List<ExamResult>();
    }
}
