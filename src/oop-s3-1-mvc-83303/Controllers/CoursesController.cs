using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using oop_s3_1_mvc_83303.Data;
using oop_s3_1_mvc_83303.Models;

namespace oop_s3_1_mvc_83303.Controllers
{
    [Authorize(Roles = "Admin,Faculty")]
    public class CoursesController : BaseController
    {
        public CoursesController(ApplicationDbContext context, UserManager<IdentityUser> userManager) : base(context, userManager) { }

        public async Task<IActionResult> Index()
        {
            if (IsAdmin)
                return View(await _context.Courses.Include(c => c.Branch).Include(c => c.Faculty).ToListAsync());
            
            var facultyId = await GetFacultyProfileId();
            return View(await _context.Courses.Include(c => c.Branch).Where(c => c.FacultyProfileId == facultyId).ToListAsync());
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
