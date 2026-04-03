using System.ComponentModel.DataAnnotations;

namespace oop_s3_1_mvc_83303.Models
{
    public class Branch
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Branch name is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [StringLength(255, ErrorMessage = "Address cannot exceed 255 characters")]
        public string Address { get; set; } = string.Empty;

        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}
