using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using oop_s3_1_mvc_83303.Data;
using oop_s3_1_mvc_83303.Models;

namespace oop_s3_1_mvc_83303.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CourseEnrolmentsController : BaseController
    {
        public CourseEnrolmentsController(ApplicationDbContext context, UserManager<IdentityUser> userManager) : base(context, userManager) { }

        public async Task<IActionResult> Index() => View(await _context.CourseEnrolments.Include(e => e.Course).Include(e => e.Student).ToListAsync());

        public async Task<IActionResult> Create()
        {
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Name");
            ViewData["StudentProfileId"] = new SelectList(_context.StudentProfiles, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CourseEnrolment enrolment)
        {
            enrolment.EnrolDate = DateTime.Now;
            enrolment.Status = "Active";
            _context.Add(enrolment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
