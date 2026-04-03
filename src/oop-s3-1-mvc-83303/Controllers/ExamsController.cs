using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using oop_s3_1_mvc_83303.Data;
using oop_s3_1_mvc_83303.Models;

namespace oop_s3_1_mvc_83303.Controllers
{
    [Authorize(Roles = "Admin,Faculty")]
    public class ExamsController : BaseController
    {
        public ExamsController(ApplicationDbContext context, UserManager<IdentityUser> userManager) : base(context, userManager) { }

        public async Task<IActionResult> Index() => View(await _context.Exams.Include(e => e.Course).ToListAsync());

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleRelease(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam != null)
            {
                exam.ResultsReleased = !exam.ResultsReleased;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
