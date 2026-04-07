using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using oop_s3_1_mvc_83303.Models;

namespace oop_s3_1_mvc_83303.Data
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static class DbSeeder
    {
        public static async Task Seed(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            context.Database.Migrate();

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
            var facultyEmails = new[] { "faculty1@college.com", "faculty2@college.com" };
            var facultyNames = new[] { "Dr. John Doe", "Dr. Jane Smith" };
            for (int i = 0; i < facultyEmails.Length; i++)
            {
                var email = facultyEmails[i];
                if (await userManager.FindByEmailAsync(email) == null)
                {
                    var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                    await userManager.CreateAsync(user, "Faculty123!");
                    await userManager.AddToRoleAsync(user, "Faculty");

                    var profile = new FacultyProfile
                    {
                        IdentityUserId = user.Id,
                        Name = facultyNames[i],
                        Email = email,
                        Phone = $"123-456-789{i}"
                    };
                    context.FacultyProfiles.Add(profile);
                }
            }
            await context.SaveChangesAsync();

            // Seed Students
            for (int i = 1; i <= 3; i++)
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
                        DOB = DateTime.Now.AddYears(-20 - i),
                        StudentNumber = $"S2026{i:D4}"
                    };
                    context.StudentProfiles.Add(studentProfile);
                }
            }
            await context.SaveChangesAsync();

            // Seed Branches
            if (!context.Branches.Any())
            {
                var branches = new List<Branch>
                {
                    new Branch { Name = "Main Campus", Address = "123 Education Ave" },
                    new Branch { Name = "Tech Wing", Address = "456 Innovation Blvd" },
                    new Branch { Name = "Art District", Address = "789 Creative Lane" }
                };
                context.Branches.AddRange(branches);
                await context.SaveChangesAsync();
            }

            // Seed Courses
            if (!context.Courses.Any())
            {
                var branches = context.Branches.ToList();
                var faculties = context.FacultyProfiles.ToList();

                var courses = new List<Course>
                {
                    new Course { Name = "Advanced C#", BranchId = branches[0].Id, FacultyProfileId = faculties[0].Id, StartDate = DateTime.Now.AddMonths(-2), EndDate = DateTime.Now.AddMonths(2) },
                    new Course { Name = "Database Systems", BranchId = branches[1].Id, FacultyProfileId = faculties[1].Id, StartDate = DateTime.Now.AddMonths(-1), EndDate = DateTime.Now.AddMonths(3) },
                    new Course { Name = "Web Development", BranchId = branches[0].Id, FacultyProfileId = faculties[0].Id, StartDate = DateTime.Now.AddMonths(-3), EndDate = DateTime.Now.AddMonths(1) }
                };
                context.Courses.AddRange(courses);
                await context.SaveChangesAsync();

                // Seed Enrolments
                var students = context.StudentProfiles.ToList();
                foreach (var course in courses)
                {
                    foreach (var student in students)
                    {
                        var enrolment = new CourseEnrolment
                        {
                            CourseId = course.Id,
                            StudentProfileId = student.Id,
                            EnrolDate = DateTime.Now.AddMonths(-1),
                            Status = "Active"
                        };
                        context.CourseEnrolments.Add(enrolment);
                    }
                }
                await context.SaveChangesAsync();

                var allEnrolments = context.CourseEnrolments.ToList();

                // Seed Attendance for each student in each course
                foreach (var enrolment in allEnrolments)
                {
                    for (int w = 1; w <= 4; w++)
                    {
                        context.AttendanceRecords.Add(new AttendanceRecord
                        {
                            CourseEnrolmentId = enrolment.Id,
                            WeekNumber = w,
                            Date = DateTime.Now.AddDays(-7 * (4 - w)),
                            IsPresent = true
                        });
                    }
                }

                // Seed Exams and Results
                foreach (var course in courses)
                {
                    var examReleased = new Exam
                    {
                        CourseId = course.Id,
                        Title = "Midterm - Released",
                        Date = DateTime.Now.AddDays(-10),
                        MaxScore = 100,
                        ResultsReleased = true
                    };
                    var examProvisional = new Exam
                    {
                        CourseId = course.Id,
                        Title = "Final - Provisional",
                        Date = DateTime.Now.AddDays(-2),
                        MaxScore = 100,
                        ResultsReleased = false
                    };
                    context.Exams.AddRange(examReleased, examProvisional);
                    await context.SaveChangesAsync();

                    foreach (var student in students)
                    {
                        context.ExamResults.Add(new ExamResult
                        {
                            ExamId = examReleased.Id,
                            StudentProfileId = student.Id,
                            Score = 75 + student.Id,
                            Grade = "B"
                        });
                        context.ExamResults.Add(new ExamResult
                        {
                            ExamId = examProvisional.Id,
                            StudentProfileId = student.Id,
                            Score = 80 + student.Id,
                            Grade = "A"
                        });
                    }
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
