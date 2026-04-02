using Microsoft.AspNetCore.Identity;

namespace oop_s3_1_mvc_83303.Models
{
    public class StudentProfile
    {
        public int Id { get; set; }
        public string IdentityUserId { get; set; } = string.Empty;
        public virtual IdentityUser? IdentityUser { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime DOB { get; set; }
        public string StudentNumber { get; set; } = string.Empty;

        public virtual ICollection<CourseEnrolment> Enrolments { get; set; } = new List<CourseEnrolment>();
        public virtual ICollection<AssignmentResult> AssignmentResults { get; set; } = new List<AssignmentResult>();
        public virtual ICollection<ExamResult> ExamResults { get; set; } = new List<ExamResult>();
    }
}
