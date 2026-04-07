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

        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var examResult = await _context.ExamResults.Include(e => e.Exam).FirstOrDefaultAsync(m => m.Id == id);
            if (examResult == null) return NotFound();

            if (IsFaculty)
            {
                var facultyId = await GetFacultyProfileId();
                if (examResult.Exam == null) return NotFound();
                var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == examResult.Exam.CourseId);
                if (course?.FacultyProfileId != facultyId) return Forbid();
            }

            return View(examResult);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Faculty")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ExamResult examResult)
        {
            if (id != examResult.Id) return NotFound();

            if (IsFaculty)
            {
                var originalResult = await _context.ExamResults.Include(e => e.Exam).AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
                if (originalResult?.Exam == null) return NotFound();
                var facultyId = await GetFacultyProfileId();
                var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == originalResult.Exam.CourseId);
                if (course?.FacultyProfileId != facultyId) return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(examResult);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.ExamResults.Any(e => e.Id == examResult.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(examResult);
        }

        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var examResult = await _context.ExamResults
                .Include(e => e.Exam)
                .Include(e => e.Student)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (examResult == null) return NotFound();

            if (IsFaculty)
            {
                var facultyId = await GetFacultyProfileId();
                if (examResult.Exam == null) return NotFound();
                var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == examResult.Exam.CourseId);
                if (course?.FacultyProfileId != facultyId) return Forbid();
            }

            return View(examResult);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin,Faculty")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var examResult = await _context.ExamResults.Include(e => e.Exam).FirstOrDefaultAsync(m => m.Id == id);
            if (examResult == null) return NotFound();

            if (IsFaculty)
            {
                var facultyId = await GetFacultyProfileId();
                if (examResult.Exam == null) return NotFound();
                var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == examResult.Exam.CourseId);
                if (course?.FacultyProfileId != facultyId) return Forbid();
            }

            _context.ExamResults.Remove(examResult);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
