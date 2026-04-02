using Microsoft.AspNetCore.Identity;

namespace oop_s3_1_mvc_83303.Models
{
    public class FacultyProfile
    {
        public int Id { get; set; }
        public string IdentityUserId { get; set; } = string.Empty;
        public virtual IdentityUser? IdentityUser { get; set; }
        
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}
