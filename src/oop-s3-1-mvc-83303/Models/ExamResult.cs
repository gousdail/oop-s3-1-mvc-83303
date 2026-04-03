using System.ComponentModel.DataAnnotations;

namespace oop_s3_1_mvc_83303.Models
{
    public class ExamResult
    {
        public int Id { get; set; }

        [Required]
        public int ExamId { get; set; }
        public virtual Exam? Exam { get; set; }

        [Required]
        public int StudentProfileId { get; set; }
        public virtual StudentProfile? Student { get; set; }

        [Required(ErrorMessage = "Score is required")]
        [Range(0, 1000, ErrorMessage = "Score must be positive")]
        public double Score { get; set; }

        [Required(ErrorMessage = "Grade is required")]
        [StringLength(2, ErrorMessage = "Grade cannot exceed 2 characters")]
        public string Grade { get; set; } = string.Empty;
    }
}
