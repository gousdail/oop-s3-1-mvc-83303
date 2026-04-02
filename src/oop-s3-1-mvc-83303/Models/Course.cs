namespace oop_s3_1_mvc_83303.Models
{
    public class Course
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int BranchId { get; set; }
        public virtual Branch? Branch { get; set; }

        public int? FacultyProfileId { get; set; }
        public virtual FacultyProfile? Faculty { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public virtual ICollection<CourseEnrolment> Enrolments { get; set; } = new List<CourseEnrolment>();
        public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public virtual ICollection<Exam> Exams { get; set; } = new List<Exam>();
    }
}
