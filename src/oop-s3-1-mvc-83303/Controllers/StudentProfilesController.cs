using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using oop_s3_1_mvc_83303.Data;
using oop_s3_1_mvc_83303.Models;

namespace oop_s3_1_mvc_83303.Controllers
{
    [Authorize(Roles = "Admin,Faculty,Student")]
    public class StudentProfilesController : BaseController
    {
        public StudentProfilesController(ApplicationDbContext context, UserManager<IdentityUser> userManager) 
            : base(context, userManager) { }

        public async Task<IActionResult> Index()
        {
            if (IsAdmin)
            {
                return View(await _context.StudentProfiles.ToListAsync());
            }
            else if (IsFaculty)
            {
                var facultyId = await GetFacultyProfileId();
                var students = await _context.CourseEnrolments
                    .Where(e => e.Course!.FacultyProfileId == facultyId)
                    .Select(e => e.Student)
                    .Distinct()
                    .ToListAsync();
                return View(students);
            }
            else // Student
            {
                var studentId = await GetStudentProfileId();
                var students = await _context.StudentProfiles.Where(s => s.Id == studentId).ToListAsync();
                return View(students);
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.StudentProfiles
                .FirstOrDefaultAsync(m => m.Id == id);

            if (student == null) return NotFound();

            // Security Check
            if (IsStudent)
            {
                var studentId = await GetStudentProfileId();
                if (student.Id != studentId) return Forbid();
            }
            else if (IsFaculty)
            {
                var facultyId = await GetFacultyProfileId();
                var isAuthorized = await _context.CourseEnrolments
                    .AnyAsync(e => e.StudentProfileId == id && e.Course!.FacultyProfileId == facultyId);
                if (!isAuthorized) return Forbid();
            }

            return View(student);
        }

        // Add Create/Edit/Delete restricted to Admin only
        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StudentProfile studentProfile)
        {
            if (ModelState.IsValid)
            {
                _context.Add(studentProfile);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(studentProfile);
        }
    }
}
