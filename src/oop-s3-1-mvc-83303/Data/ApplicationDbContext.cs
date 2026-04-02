using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using oop_s3_1_mvc_83303.Models;

namespace oop_s3_1_mvc_83303.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Branch> Branches { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<StudentProfile> StudentProfiles { get; set; }
        public DbSet<FacultyProfile> FacultyProfiles { get; set; }
        public DbSet<CourseEnrolment> CourseEnrolments { get; set; }
        public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<AssignmentResult> AssignmentResults { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamResult> ExamResults { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Cascade delete configurations if necessary, or restrict
            builder.Entity<CourseEnrolment>()
                .HasOne(ce => ce.Student)
                .WithMany(s => s.Enrolments)
                .HasForeignKey(ce => ce.StudentProfileId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<AssignmentResult>()
                .HasOne(ar => ar.Student)
                .WithMany(s => s.AssignmentResults)
                .HasForeignKey(ar => ar.StudentProfileId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ExamResult>()
                .HasOne(er => er.Student)
                .WithMany(s => s.ExamResults)
                .HasForeignKey(er => er.StudentProfileId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
