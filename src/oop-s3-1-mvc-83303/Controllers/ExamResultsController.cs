using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using oop_s3_1_mvc_83303.Data;
using oop_s3_1_mvc_83303.Models;

namespace oop_s3_1_mvc_83303.Controllers
{
    [Authorize(Roles = "Admin,Faculty,Student")]
    public class ExamResultsController : BaseController
    {
        public ExamResultsController(ApplicationDbContext context, UserManager<IdentityUser> userManager) 
            : base(context, userManager) { }

        public async Task<IActionResult> Index()
        {
            if (IsAdmin)
            {
                return View(await _context.ExamResults.Include(er => er.Exam).Include(er => er.Student).ToListAsync());
            }
            else if (IsFaculty)
            {
                var facultyId = await GetFacultyProfileId();
                return View(await _context.ExamResults
                    .Include(er => er.Exam)
                    .Include(er => er.Student)
                    .Where(er => er.Exam != null && er.Exam.Course != null && er.Exam.Course.FacultyProfileId == facultyId)
                    .ToListAsync());
            }
            else // Student
            {
                var studentId = await GetStudentProfileId();
                // Critical Rule: Student must NOT see provisional results (ResultsReleased == false)
                return View(await _context.ExamResults
                    .Include(er => er.Exam)
                    .Where(er => er.StudentProfileId == studentId && er.Exam != null && er.Exam.ResultsReleased == true)
                    .ToListAsync());
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var examResult = await _context.ExamResults
                .Include(e => e.Exam)
                .Include(e => e.Student)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (examResult == null) return NotFound();

            if (IsStudent)
            {
                var studentId = await GetStudentProfileId();
                if (examResult.StudentProfileId != studentId) return Forbid();
                if (examResult.Exam == null || !examResult.Exam.ResultsReleased) return Forbid(); // Strict Rule
            }
            else if (IsFaculty)
            {
                var facultyId = await GetFacultyProfileId();
                if (examResult.Exam == null) return NotFound();
                var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == examResult.Exam.CourseId);
                if (course?.FacultyProfileId != facultyId) return Forbid();
            }

            return View(examResult);
        }

        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> Create()
        {
            // Implementation for creating results
            return View();
        }
    }
}
