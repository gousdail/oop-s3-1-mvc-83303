using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace oop_s3_1_mvc_83303.Models
{
    public class FacultyProfile
    {
        public int Id { get; set; }

        [Required]
        public string IdentityUserId { get; set; } = string.Empty;
        public virtual IdentityUser? IdentityUser { get; set; }

        [Required(ErrorMessage = "Faculty name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string Phone { get; set; } = string.Empty;

        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}
