using System.ComponentModel.DataAnnotations;

namespace oop_s3_1_mvc_83303.Models
{
    public class Exam
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Course is required")]
        public int CourseId { get; set; }
        public virtual Course? Course { get; set; }

        [Required(ErrorMessage = "Exam title is required")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Exam date is required")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Maximum score is required")]
        [Range(0, 1000, ErrorMessage = "Score must be between 0 and 1000")]
        public double MaxScore { get; set; }

        public bool ResultsReleased { get; set; }

        public virtual ICollection<ExamResult> Results { get; set; } = new List<ExamResult>();
    }
}
