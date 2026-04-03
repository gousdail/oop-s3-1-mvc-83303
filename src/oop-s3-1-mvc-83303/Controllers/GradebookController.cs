using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using oop_s3_1_mvc_83303.Data;
using oop_s3_1_mvc_83303.Models;

namespace oop_s3_1_mvc_83303.Controllers
{
    [Authorize(Roles = "Admin,Faculty")]
    public class GradebookController : BaseController
    {
        public GradebookController(ApplicationDbContext context, UserManager<IdentityUser> userManager) : base(context, userManager) { }

        public async Task<IActionResult> Index(int courseId)
        {
            var course = await _context.Courses
                .Include(c => c.Assignments).ThenInclude(a => a.Results)
                .Include(c => c.Exams).ThenInclude(e => e!.Results)
                .Include(c => c.Enrolments).ThenInclude(en => en!.Student)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null) return NotFound();

            // Security: Faculty can only see their own course gradebook
            if (IsFaculty)
            {
                var facultyId = await GetFacultyProfileId();
                if (course.FacultyProfileId != facultyId) return Forbid();
            }

            return View(course);
        }
    }
}
