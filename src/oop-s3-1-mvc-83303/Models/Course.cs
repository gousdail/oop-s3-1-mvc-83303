using System.ComponentModel.DataAnnotations;

namespace oop_s3_1_mvc_83303.Models
{
    public class Course
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Course name is required")]
        [StringLength(100, ErrorMessage = "Course name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Branch is required")]
        public int BranchId { get; set; }
        public virtual Branch? Branch { get; set; }

        public int? FacultyProfileId { get; set; }
        public virtual FacultyProfile? Faculty { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        public virtual ICollection<CourseEnrolment> Enrolments { get; set; } = new List<CourseEnrolment>();
        public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public virtual ICollection<Exam> Exams { get; set; } = new List<Exam>();
    }
}
