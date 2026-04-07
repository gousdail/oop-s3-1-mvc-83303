using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using oop_s3_1_mvc_83303.Controllers;
using oop_s3_1_mvc_83303.Data;
using oop_s3_1_mvc_83303.Models;
using System.Security.Claims;
using Xunit;

namespace oop_s3_1_mvc_83303.Tests
{
    public class ControllerTests
    {
        private ApplicationDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private Mock<UserManager<IdentityUser>> GetMockUserManager()
        {
            var store = new Mock<IUserStore<IdentityUser>>();
            var userManager = new Mock<UserManager<IdentityUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            return userManager;
        }

        private void SetupUser(Controller controller, Mock<UserManager<IdentityUser>> userManager, string userId, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, userId),
                new Claim(ClaimTypes.Role, role)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            userManager.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
        }

        [Fact]
        public async Task AttendanceRecordsController_Index_Admin_ReturnsAllRecords()
        {
            // Arrange
            var context = GetDbContext();
            var userManager = GetMockUserManager();
            var controller = new AttendanceRecordsController(context, userManager.Object);
            SetupUser(controller, userManager, "admin-id", "Admin");

            var student = new StudentProfile { Name = "Student", Email = "s@c.com", Phone = "123", Address = "Add", StudentNumber = "S1", IdentityUserId = "s-id" };
            var course = new Course { Name = "Course", StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(1) };
            context.StudentProfiles.Add(student);
            context.Courses.Add(course);
            await context.SaveChangesAsync();

            var enrolment = new CourseEnrolment { StudentProfileId = student.Id, CourseId = course.Id };
            context.CourseEnrolments.Add(enrolment);
            await context.SaveChangesAsync();

            context.AttendanceRecords.Add(new AttendanceRecord { CourseEnrolmentId = enrolment.Id, WeekNumber = 1, Date = DateTime.Now, IsPresent = true });
            await context.SaveChangesAsync();

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<AttendanceRecord>>(viewResult.ViewData.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task AttendanceRecordsController_Index_Faculty_ReturnsOnlyOwnCourseRecords()
        {
            // Arrange
            var context = GetDbContext();
            var userManager = GetMockUserManager();
            var facultyUserId = "faculty-user-id";

            var controller = new AttendanceRecordsController(context, userManager.Object);
            SetupUser(controller, userManager, facultyUserId, "Faculty");

            var facultyProfile = new FacultyProfile { Name = "Dr. Test", Email = "f@c.com", Phone = "123", IdentityUserId = facultyUserId };
            context.FacultyProfiles.Add(facultyProfile);
            await context.SaveChangesAsync();

            var course1 = new Course { Name = "Own Course", FacultyProfileId = facultyProfile.Id, StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(1) };
            var course2 = new Course { Name = "Other Course", FacultyProfileId = facultyProfile.Id + 1, StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(1) };
            context.Courses.AddRange(course1, course2);
            await context.SaveChangesAsync();

            var student = new StudentProfile { Name = "Student", Email = "s@c.com", Phone = "123", Address = "Add", StudentNumber = "S1", IdentityUserId = "s-id" };
            context.StudentProfiles.Add(student);
            await context.SaveChangesAsync();

            var enrolment1 = new CourseEnrolment { StudentProfileId = student.Id, CourseId = course1.Id };
            var enrolment2 = new CourseEnrolment { StudentProfileId = student.Id, CourseId = course2.Id };
            context.CourseEnrolments.AddRange(enrolment1, enrolment2);
            await context.SaveChangesAsync();

            context.AttendanceRecords.Add(new AttendanceRecord { CourseEnrolmentId = enrolment1.Id, WeekNumber = 1, Date = DateTime.Now, IsPresent = true });
            context.AttendanceRecords.Add(new AttendanceRecord { CourseEnrolmentId = enrolment2.Id, WeekNumber = 1, Date = DateTime.Now, IsPresent = true });
            await context.SaveChangesAsync();

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<AttendanceRecord>>(viewResult.ViewData.Model);
            Assert.Single(model);
            Assert.Equal(course1.Id, model.First().Enrolment!.CourseId);
        }

        [Fact]
        public async Task AttendanceRecordsController_Details_NotFound_IfMissing()
        {
            var context = GetDbContext();
            var userManager = GetMockUserManager();
            var controller = new AttendanceRecordsController(context, userManager.Object);
            SetupUser(controller, userManager, "admin-id", "Admin");

            var result = await controller.Details(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CoursesController_Index_Faculty_ReturnsAssignedCourses()
        {
            // Arrange
            var context = GetDbContext();
            var userManager = GetMockUserManager();
            var facultyUserId = "faculty-user-id";

            var controller = new CoursesController(context, userManager.Object);
            SetupUser(controller, userManager, facultyUserId, "Faculty");

            var facultyProfile = new FacultyProfile { Name = "Dr. Test", Email = "f@college.com", Phone = "123", IdentityUserId = facultyUserId };
            context.FacultyProfiles.Add(facultyProfile);
            await context.SaveChangesAsync();

            var course1 = new Course { Name = "Own Course", FacultyProfileId = facultyProfile.Id, StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(1) };
            var course2 = new Course { Name = "Other Course", FacultyProfileId = facultyProfile.Id + 1, StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(1) };
            context.Courses.AddRange(course1, course2);
            await context.SaveChangesAsync();

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Course>>(viewResult.ViewData.Model);
            Assert.Single(model);
            Assert.Equal(course1.Id, model.First().Id);
        }

        [Fact]
        public async Task StudentProfilesController_Index_Student_ReturnsOwnProfileOnly()
        {
            var context = GetDbContext();
            var userManager = GetMockUserManager();
            var studentUserId = "s-user";

            var controller = new StudentProfilesController(context, userManager.Object);
            SetupUser(controller, userManager, studentUserId, "Student");

            var s1 = new StudentProfile { Name = "S1", Email = "s1@college.com", Phone = "1", Address = "A1", DOB = DateTime.Now.AddYears(-20), StudentNumber = "S1", IdentityUserId = studentUserId };
            var s2 = new StudentProfile { Name = "S2", Email = "s2@college.com", Phone = "2", Address = "A2", DOB = DateTime.Now.AddYears(-20), StudentNumber = "S2", IdentityUserId = "other" };
            context.StudentProfiles.AddRange(s1, s2);
            await context.SaveChangesAsync();

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<StudentProfile>>(viewResult.ViewData.Model);
            Assert.Single(model);
            Assert.Equal("S1", model.First().Name);
        }

        [Fact]
        public async Task StudentProfilesController_Index_Faculty_ReturnsAssignedStudents()
        {
            var context = GetDbContext();
            var userManager = GetMockUserManager();
            var facultyUserId = "f1-user";

            var controller = new StudentProfilesController(context, userManager.Object);
            SetupUser(controller, userManager, facultyUserId, "Faculty");

            var profile = new FacultyProfile { Name = "Faculty", Email = "f1@college.com", Phone = "123", IdentityUserId = facultyUserId };
            context.FacultyProfiles.Add(profile);
            await context.SaveChangesAsync();

            var course = new Course { Name = "C1", FacultyProfileId = profile.Id, StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(1) };
            context.Courses.Add(course);
            await context.SaveChangesAsync();

            var s1 = new StudentProfile { Name = "S1", Email = "s1@college.com", Phone = "1", Address = "A1", DOB = DateTime.Now.AddYears(-20), StudentNumber = "S1", IdentityUserId = "u1" };
            var s2 = new StudentProfile { Name = "S2", Email = "s2@college.com", Phone = "2", Address = "A2", DOB = DateTime.Now.AddYears(-20), StudentNumber = "S2", IdentityUserId = "u2" };
            context.StudentProfiles.AddRange(s1, s2);
            await context.SaveChangesAsync();

            context.CourseEnrolments.Add(new CourseEnrolment { CourseId = course.Id, StudentProfileId = s1.Id });
            await context.SaveChangesAsync();

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<StudentProfile>>(viewResult.ViewData.Model);
            Assert.Single(model);
            Assert.Equal("S1", model.First().Name);
        }
    }
}
