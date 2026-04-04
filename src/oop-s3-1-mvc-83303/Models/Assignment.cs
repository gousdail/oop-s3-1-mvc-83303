namespace oop_s3_1_mvc_83303.Models
{
    public class Assignment
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public virtual Course? Course { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public string Title { get; set; } = string.Empty;
        public double MaxScore { get; set; }
        public DateTime DueDate { get; set; }

        public virtual ICollection<AssignmentResult> Results { get; set; } = new List<AssignmentResult>();
    }
}
