using Microsoft.AspNetCore.Identity;
using oop_s3_1_mvc_83303.Models;

namespace oop_s3_1_mvc_83303.Data
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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
                    StartDate = DateTime.Now.AddMonths(-1),
                    EndDate = DateTime.Now.AddMonths(3)
                };
                context.Courses.Add(course);
                await context.SaveChangesAsync();

                // Seed Enrolments
                var students = context.StudentProfiles.ToList();
                var enrolments = new List<CourseEnrolment>();
                foreach (var student in students)
                {
                    var enrolment = new CourseEnrolment
                    {
                        CourseId = course.Id,
                        StudentProfileId = student.Id,
                        EnrolDate = DateTime.Now.AddMonths(-1),
                        Status = "Active"
                    };
                    enrolments.Add(enrolment);
                    context.CourseEnrolments.Add(enrolment);
                }
                await context.SaveChangesAsync();

                // Seed Attendance for 4 weeks
                foreach (var enrolment in enrolments)
                {
                    for (int w = 1; w <= 4; w++)
                    {
                        context.AttendanceRecords.Add(new AttendanceRecord
                        {
                            CourseEnrolmentId = enrolment.Id,
                            WeekNumber = w,
                            Date = DateTime.Now.AddDays(-7 * (4 - w)),
                            IsPresent = (w % 4 != 0) // Present for weeks 1,2,3, absent for week 4
                        });
                    }
                }

                // Seed an Exam
                var exam = new Exam
                {
                    CourseId = course.Id,
                    Title = "Midterm Exam",
                    Date = DateTime.Now.AddDays(-5),
                    MaxScore = 100,
                    ResultsReleased = true
                };
                context.Exams.Add(exam);
                await context.SaveChangesAsync();

                // Seed Exam Results
                foreach (var enrolment in enrolments)
                {
                    context.ExamResults.Add(new ExamResult
                    {
                        ExamId = exam.Id,
                        StudentProfileId = enrolment.StudentProfileId,
                        Score = enrolment.StudentProfileId % 2 == 0 ? 85 : 72,
                        Grade = enrolment.StudentProfileId % 2 == 0 ? "A" : "B"
                    });
                }

                // Seed an Assignment
                var assignment = new Assignment
                {
                    CourseId = course.Id,
                    Title = "First Project",
                    MaxScore = 50,
                    DueDate = DateTime.Now.AddDays(-10)
                };
                context.Assignments.Add(assignment);
                await context.SaveChangesAsync();

                // Seed Assignment Results
                foreach (var enrolment in enrolments)
                {
                    context.AssignmentResults.Add(new AssignmentResult
                    {
                        AssignmentId = assignment.Id,
                        StudentProfileId = enrolment.StudentProfileId,
                        Score = 42,
                        Feedback = "Good work, keep it up!"
                    });
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
