namespace oop_s3_1_mvc_83303.Models
{
    public class ExamResult
    {
        public int Id { get; set; }
        public int ExamId { get; set; }
        public virtual Exam? Exam { get; set; }

        public int StudentProfileId { get; set; }
        public virtual StudentProfile? Student { get; set; }

        public double Score { get; set; }
        public string Grade { get; set; } = string.Empty;
    }
}
