using Microsoft.AspNetCore.Identity;
using oop_s3_1_mvc_83303.Models;

namespace oop_s3_1_mvc_83303.Data
{
    public static class DbSeeder
    {
        public static async Task Seed(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            context.Database.EnsureCreated();

            // Seed Roles
            string[] roles = { "Admin", "Faculty", "Student" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed Admin
            var adminEmail = "admin@college.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                await userManager.CreateAsync(admin, "Admin123!");
                await userManager.AddToRoleAsync(admin, "Admin");
            }

            // Seed Faculty
            var facultyEmail = "faculty@college.com";
            if (await userManager.FindByEmailAsync(facultyEmail) == null)
            {
                var facultyUser = new IdentityUser { UserName = facultyEmail, Email = facultyEmail, EmailConfirmed = true };
                await userManager.CreateAsync(facultyUser, "Faculty123!");
                await userManager.AddToRoleAsync(facultyUser, "Faculty");

                var facultyProfile = new FacultyProfile
                {
                    IdentityUserId = facultyUser.Id,
                    Name = "Dr. John Doe",
                    Email = facultyEmail,
                    Phone = "123-456-7890"
                };
                context.FacultyProfiles.Add(facultyProfile);
            }

            // Seed Students
            for (int i = 1; i <= 2; i++)
            {
                var studentEmail = $"student{i}@college.com";
                if (await userManager.FindByEmailAsync(studentEmail) == null)
                {
                    var studentUser = new IdentityUser { UserName = studentEmail, Email = studentEmail, EmailConfirmed = true };
                    await userManager.CreateAsync(studentUser, "Student123!");
                    await userManager.AddToRoleAsync(studentUser, "Student");

                    var studentProfile = new StudentProfile
                    {
                        IdentityUserId = studentUser.Id,
                        Name = $"Student {i}",
                        Email = studentEmail,
                        Phone = $"000-000-000{i}",
                        Address = $"{i} College Street",
                        DOB = DateTime.Now.AddYears(-20),
                        StudentNumber = $"S0000{i}"
                    };
                    context.StudentProfiles.Add(studentProfile);
                }
            }

            // Seed Branch and Course if empty
            if (!context.Branches.Any())
            {
                var branch = new Branch { Name = "Main Campus", Address = "123 Education Ave" };
                context.Branches.Add(branch);
                await context.SaveChangesAsync();

                var faculty = context.FacultyProfiles.First();
                var course = new Course 
                { 
                    Name = "Modern Programming", 
                    BranchId = branch.Id, 
                    FacultyProfileId = faculty.Id,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddMonths(4)
                };
                context.Courses.Add(course);
            }

            await context.SaveChangesAsync();
        }
    }
}
