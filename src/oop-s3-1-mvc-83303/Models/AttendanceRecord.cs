namespace oop_s3_1_mvc_83303.Models
{
    public class AttendanceRecord
    {
        public int Id { get; set; }
        public int CourseEnrolmentId { get; set; }
        public virtual CourseEnrolment? Enrolment { get; set; }

        public DateTime Date { get; set; }
        public int WeekNumber { get; set; }
        public bool IsPresent { get; set; }
    }
}
