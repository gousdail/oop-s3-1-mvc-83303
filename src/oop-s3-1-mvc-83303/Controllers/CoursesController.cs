using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using oop_s3_1_mvc_83303.Data;
using oop_s3_1_mvc_83303.Models;

namespace oop_s3_1_mvc_83303.Controllers
{
    [Authorize(Roles = "Admin,Faculty,Student")]
    public class CoursesController : BaseController
    {
        public CoursesController(ApplicationDbContext context, UserManager<IdentityUser> userManager) : base(context, userManager) { }

        public async Task<IActionResult> Index()
        {
            if (IsAdmin)
                return View(await _context.Courses.Include(c => c.Branch).Include(c => c.Faculty).ToListAsync());
            
            if (IsFaculty)
            {
                var facultyId = await GetFacultyProfileId();
                return View(await _context.Courses.Include(c => c.Branch).Where(c => c.FacultyProfileId == facultyId).ToListAsync());
            }

            // Student
            var studentId = await GetStudentProfileId();
            var courses = await _context.CourseEnrolments
                .Include(e => e.Course).ThenInclude(c => c.Branch)
                .Include(e => e.Course).ThenInclude(c => c.Faculty)
                .Where(e => e.StudentProfileId == studentId)
                .Select(e => e.Course)
                .ToListAsync();

            return View(courses);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.Branch)
                .Include(c => c.Faculty)
                .Include(c => c.Enrolments).ThenInclude(e => e.Student)
                .Include(c => c.Exams)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null) return NotFound();

            if (IsStudent)
            {
                var studentId = await GetStudentProfileId();
                var isEnrolled = await _context.CourseEnrolments.AnyAsync(e => e.CourseId == id && e.StudentProfileId == studentId);
                if (!isEnrolled) return Forbid();
            }
            else if (IsFaculty)
            {
                var facultyId = await GetFacultyProfileId();
                if (course.FacultyProfileId != facultyId) return Forbid();
            }

            return View(course);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create() 
        {
            ViewBag.Branches = _context.Branches.ToList();
            ViewBag.Faculties = _context.FacultyProfiles.ToList();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Course course)
        {
            if (ModelState.IsValid)
            {
                _context.Add(course);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(course);
        }
    }
}
