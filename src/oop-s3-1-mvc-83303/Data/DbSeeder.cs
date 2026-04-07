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
            var facultyEmails = new[] { "dublin_dean@ireland_college.com", "cork_dean@ireland_college.com", "galway_dean@ireland_college.com" };
            var facultyNames = new[] { "Dr. Liam O'Connor", "Dr. Aoife Murphy", "Dr. Seamus Byrne" };
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
                        Phone = $"087-{i:D3}-1234"
                    };
                    context.FacultyProfiles.Add(profile);
                }
            }
            await context.SaveChangesAsync();

            // Seed Students
            for (int i = 1; i <= 5; i++)
            {
                var studentEmail = $"student{i}@ireland_college.com";
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
                        Phone = $"089-000-000{i}",
                        Address = $"{i} St. Patrick Street",
                        DOB = DateTime.Now.AddYears(-20 - i),
                        StudentNumber = $"S2026{i:D4}"
                    };
                    context.StudentProfiles.Add(studentProfile);
                }
            }
            await context.SaveChangesAsync();

            // Seed 3 Irish Branches
            if (!context.Branches.Any())
            {
                var branches = new List<Branch>
                {
                    new Branch { Name = "Dublin Campus", Address = "O'Connell St, Dublin 1, Ireland" },
                    new Branch { Name = "Cork Campus", Address = "Grand Parade, Cork, Ireland" },
                    new Branch { Name = "Galway Campus", Address = "Eyre Square, Galway, Ireland" }
                };
                context.Branches.AddRange(branches);
                await context.SaveChangesAsync();
            }

            // Seed Courses - At least 2 per branch with different teachers
            if (!context.Courses.Any())
            {
                var branches = context.Branches.ToList();
                var faculties = context.FacultyProfiles.ToList();

                var courses = new List<Course>();
                // Dublin (branches[0])
                courses.Add(new Course { Name = "Irish Law Foundations", BranchId = branches[0].Id, FacultyProfileId = faculties[0].Id, StartDate = DateTime.Now.AddMonths(-2), EndDate = DateTime.Now.AddMonths(2) });
                courses.Add(new Course { Name = "Digital Marketing", BranchId = branches[0].Id, FacultyProfileId = faculties[1].Id, StartDate = DateTime.Now.AddMonths(-2), EndDate = DateTime.Now.AddMonths(2) });
                
                // Cork (branches[1])
                courses.Add(new Course { Name = "Marine Biology", BranchId = branches[1].Id, FacultyProfileId = faculties[1].Id, StartDate = DateTime.Now.AddMonths(-1), EndDate = DateTime.Now.AddMonths(3) });
                courses.Add(new Course { Name = "Food Science", BranchId = branches[1].Id, FacultyProfileId = faculties[2].Id, StartDate = DateTime.Now.AddMonths(-1), EndDate = DateTime.Now.AddMonths(3) });

                // Galway (branches[2])
                courses.Add(new Course { Name = "Traditional Music", BranchId = branches[2].Id, FacultyProfileId = faculties[2].Id, StartDate = DateTime.Now.AddMonths(-3), EndDate = DateTime.Now.AddMonths(1) });
                courses.Add(new Course { Name = "Hydraulic Engineering", BranchId = branches[2].Id, FacultyProfileId = faculties[0].Id, StartDate = DateTime.Now.AddMonths(-3), EndDate = DateTime.Now.AddMonths(1) });

                context.Courses.AddRange(courses);
                await context.SaveChangesAsync();

                // Seed Enrolments
                var students = context.StudentProfiles.ToList();
                foreach (var course in courses)
                {
                    // Enroll 3 random students in each course
                    var courseStudents = students.OrderBy(x => Guid.NewGuid()).Take(3).ToList();
                    foreach (var student in courseStudents)
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

                // Seed Attendance for each student in each course - 4 weeks
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

                    var courseEnrolments = context.CourseEnrolments.Where(e => e.CourseId == course.Id).ToList();
                    foreach (var enrolment in courseEnrolments)
                    {
                        context.ExamResults.Add(new ExamResult
                        {
                            ExamId = examReleased.Id,
                            StudentProfileId = enrolment.StudentProfileId,
                            Score = 75 + (enrolment.Id % 10),
                            Grade = "B"
                        });
                        context.ExamResults.Add(new ExamResult
                        {
                            ExamId = examProvisional.Id,
                            StudentProfileId = enrolment.StudentProfileId,
                            Score = 80 + (enrolment.Id % 10),
                            Grade = "A"
                        });
                    }
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
