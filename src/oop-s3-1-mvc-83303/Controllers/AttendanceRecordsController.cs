using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using oop_s3_1_mvc_83303.Data;
using oop_s3_1_mvc_83303.Models;

namespace oop_s3_1_mvc_83303.Controllers
{
    [Authorize(Roles = "Admin,Faculty")]
    public class AttendanceRecordsController : BaseController
    {
        public AttendanceRecordsController(ApplicationDbContext context, UserManager<IdentityUser> userManager) : base(context, userManager) { }

        public async Task<IActionResult> TakeAttendance(int courseId)
        {
            var enrolments = await _context.CourseEnrolments
                .Include(e => e.Student)
                .Where(e => e.CourseId == courseId)
                .ToListAsync();
            ViewBag.CourseId = courseId;
            return View(enrolments);
        }

        [HttpPost]
        public async Task<IActionResult> SaveAttendance(int courseId, int weekNumber, Dictionary<int, bool> attendance)
        {
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
