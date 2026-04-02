namespace oop_s3_1_mvc_83303.Models
{
    public class CourseEnrolment
    {
        public int Id { get; set; }
        public int StudentProfileId { get; set; }
        public virtual StudentProfile? Student { get; set; }
        
        public int CourseId { get; set; }
        public virtual Course? Course { get; set; }

        public DateTime EnrolDate { get; set; }
        public string Status { get; set; } = "Active";

        public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    }
}
