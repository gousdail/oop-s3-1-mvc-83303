using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using oop_s3_1_mvc_83303.Data;
using Microsoft.EntityFrameworkCore;

namespace oop_s3_1_mvc_83303.Controllers
{
    public class BaseController : Controller
    {
        protected readonly ApplicationDbContext _context;
        protected readonly UserManager<IdentityUser> _userManager;

        public BaseController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        protected string CurrentUserId => _userManager.GetUserId(User) ?? string.Empty;

        protected async Task<int?> GetStudentProfileId()
        {
            var profile = await _context.StudentProfiles.FirstOrDefaultAsync(s => s.IdentityUserId == CurrentUserId);
            return profile?.Id;
        }

        protected async Task<int?> GetFacultyProfileId()
        {
            var profile = await _context.FacultyProfiles.FirstOrDefaultAsync(f => f.IdentityUserId == CurrentUserId);
            return profile?.Id;
        }

        protected bool IsAdmin => User.IsInRole("Admin");
        protected bool IsFaculty => User.IsInRole("Faculty");
        protected bool IsStudent => User.IsInRole("Student");
    }
}
