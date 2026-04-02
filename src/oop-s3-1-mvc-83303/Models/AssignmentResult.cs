namespace oop_s3_1_mvc_83303.Models
{
    public class AssignmentResult
    {
        public int Id { get; set; }
        public int AssignmentId { get; set; }
        public virtual Assignment? Assignment { get; set; }

        public int StudentProfileId { get; set; }
        public virtual StudentProfile? Student { get; set; }

        public double Score { get; set; }
        public string Feedback { get; set; } = string.Empty;
    }
}
