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
                    .Where(er => er.Exam!.Course!.FacultyProfileId == facultyId)
                    .ToListAsync());
            }
            else // Student
            {
                var studentId = await GetStudentProfileId();
                // Critical Rule: Student must NOT see provisional results (ResultsReleased == false)
                return View(await _context.ExamResults
                    .Include(er => er.Exam)
                    .Where(er => er.StudentProfileId == studentId && er.Exam!.ResultsReleased == true)
                    .ToListAsync());
            }
        }

        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> Create()
        {
            // Implementation for creating results
            return View();
        }
    }
}
