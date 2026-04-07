using System.ComponentModel.DataAnnotations;

namespace oop_s3_1_mvc_83303.Models
{
    public class StudentProfile : IValidatableObject
    {
        public int Id { get; set; }
        public string IdentityUserId { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, Phone]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required, DataType(DataType.Date)]
        public DateTime DOB { get; set; }

        [Required, StringLength(20)]
        public string StudentNumber { get; set; } = string.Empty;

        public virtual ICollection<CourseEnrolment> Enrolments { get; set; } = new List<CourseEnrolment>();

        public virtual ICollection<ExamResult> ExamResults { get; set; } = new List<ExamResult>();

        public virtual ICollection<AssignmentResult> AssignmentResults { get; set; } = new List<AssignmentResult>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DOB > DateTime.Now.AddYears(-18))
            {
                yield return new ValidationResult("Student must be at least 18 years old.", new[] { nameof(DOB) });
            }

            if (string.IsNullOrWhiteSpace(StudentNumber) || !StudentNumber.StartsWith("S"))
            {
                yield return new ValidationResult("Student number must start with 'S'.", new[] { nameof(StudentNumber) });
            }
        }
    }
}
