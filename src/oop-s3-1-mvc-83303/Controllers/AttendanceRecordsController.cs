using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using oop_s3_1_mvc_83303.Data;
using oop_s3_1_mvc_83303.Models;

namespace oop_s3_1_mvc_83303.Controllers
{
    [Authorize(Roles = "Admin,Faculty,Student")]
    public class AttendanceRecordsController : BaseController
    {
        public AttendanceRecordsController(ApplicationDbContext context, UserManager<IdentityUser> userManager) : base(context, userManager) { }

        public async Task<IActionResult> Index()
        {
            if (IsAdmin)
            {
                return View(await _context.AttendanceRecords
                    .Include(a => a.Enrolment).ThenInclude(e => e.Course)
                    .Include(a => a.Enrolment).ThenInclude(e => e.Student)
                    .ToListAsync());
            }
            else if (IsFaculty)
            {
                var facultyId = await GetFacultyProfileId();
                return View(await _context.AttendanceRecords
                    .Include(a => a.Enrolment).ThenInclude(e => e.Course)
                    .Include(a => a.Enrolment).ThenInclude(e => e.Student)
                    .Where(a => a.Enrolment!.Course!.FacultyProfileId == facultyId)
                    .ToListAsync());
            }
            else // Student
            {
                var studentId = await GetStudentProfileId();
                return View(await _context.AttendanceRecords
                    .Include(a => a.Enrolment).ThenInclude(e => e.Course)
                    .Where(a => a.Enrolment!.StudentProfileId == studentId)
                    .ToListAsync());
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var attendanceRecord = await _context.AttendanceRecords
                .Include(a => a.Enrolment).ThenInclude(e => e.Course)
                .Include(a => a.Enrolment).ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (attendanceRecord == null) return NotFound();

            if (IsStudent)
            {
                var studentId = await GetStudentProfileId();
                if (attendanceRecord.Enrolment!.StudentProfileId != studentId) return Forbid();
            }
            else if (IsFaculty)
            {
                var facultyId = await GetFacultyProfileId();
                if (attendanceRecord.Enrolment!.Course!.FacultyProfileId != facultyId) return Forbid();
            }

            return View(attendanceRecord);
        }

        [Authorize(Roles = "Admin,Faculty")]
        public async Task<IActionResult> TakeAttendance(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound();

            if (IsFaculty)
            {
                var facultyId = await GetFacultyProfileId();
                if (course.FacultyProfileId != facultyId) return Forbid();
            }

            var enrolments = await _context.CourseEnrolments
                .Include(e => e.Student)
                .Where(e => e.CourseId == courseId)
                .ToListAsync();
            ViewBag.CourseId = courseId;
            return View(enrolments);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Faculty")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAttendance(int courseId, int weekNumber, Dictionary<int, bool> attendance)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound();

            if (IsFaculty)
            {
                var facultyId = await GetFacultyProfileId();
                if (course.FacultyProfileId != facultyId) return Forbid();
            }

            foreach (var item in attendance)
            {
                var record = new AttendanceRecord
                {
                    CourseEnrolmentId = item.Key,
                    WeekNumber = weekNumber,
                    Date = DateTime.Now,
                    IsPresent = item.Value
                };
                _context.AttendanceRecords.Add(record);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Courses");
        }
    }
}
