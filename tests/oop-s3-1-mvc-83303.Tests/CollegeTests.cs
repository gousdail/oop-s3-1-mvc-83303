using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using oop_s3_1_mvc_83303.Controllers;
using oop_s3_1_mvc_83303.Data;
using oop_s3_1_mvc_83303.Models;
using oop_s3_1_mvc_83303.Services;
using System.Security.Claims;
using Xunit;

namespace oop_s3_1_mvc_83303.Tests
{
    public class CollegeTests
    {
        private readonly GradebookService _gradebookService = new GradebookService();

        private ApplicationDbContext GetContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private Mock<UserManager<IdentityUser>> GetMockUserManager()
        {
            var store = new Mock<IUserStore<IdentityUser>>();
            return new Mock<UserManager<IdentityUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }

        private void SetupController(BaseController controller, string userId, string role)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            }, "mock"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        // --- SERVICE TESTS (GradebookService) ---

        [Fact]
        public void CalculateAverage_WithValidScores_ReturnsCorrectAverage()
        {
            var scores = new List<double> { 80, 90, 70 };
            var result = _gradebookService.CalculateAverage(scores);
            Assert.Equal(80, result);
        }

        [Fact]
        public void CalculateAverage_EmptyList_ReturnsZero()
        {
            var result = _gradebookService.CalculateAverage(new List<double>());
            Assert.Equal(0, result);
        }

        [Theory]
        [InlineData(75, 100, "A")]
        [InlineData(65, 100, "B")]
        [InlineData(55, 100, "C")]
        [InlineData(45, 100, "D")]
        [InlineData(35, 100, "F")]
        public void GetGrade_ReturnsExpectedGradeBasedOnPercentage(double score, double max, string expected)
        {
            Assert.Equal(expected, _gradebookService.GetGrade(score, max));
        }

        [Fact]
        public void CanStudentSeeExamResult_CorrectlyChecksReleasedFlag()
        {
            var releasedExam = new Exam { ResultsReleased = true };
            var hiddenExam = new Exam { ResultsReleased = false };
            
            Assert.True(_gradebookService.CanStudentSeeExamResult(releasedExam));
            Assert.False(_gradebookService.CanStudentSeeExamResult(hiddenExam));
        }

        // --- CONTROLLER TESTS - BRANCHES (Admin Role) ---

        [Fact]
        public async Task Branches_Create_Post_Admin_ValidModel_RedirectsToIndex()
        {
            var db = GetContext();
            var controller = new BranchesController(db, GetMockUserManager().Object);
            SetupController(controller, "admin1", "Admin");

            var branch = new Branch { Name = "Main Campus", Address = "123 Tech Lane" };
            var result = await controller.Create(branch);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(1, await db.Branches.CountAsync());
        }

        [Fact]
        public async Task Branches_Create_Post_InvalidModel_ReturnsViewWithModel()
        {
            var db = GetContext();
            var controller = new BranchesController(db, GetMockUserManager().Object);
            SetupController(controller, "admin1", "Admin");
            controller.ModelState.AddModelError("Name", "Name is too short");

            var result = await controller.Create(new Branch());

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(0, await db.Branches.CountAsync());
        }

        // --- CONTROLLER TESTS - EXAM RESULTS (Student Visibility) ---

        [Fact]
        public async Task ExamResults_Index_Student_HidesUnreleasedResults()
        {
            var db = GetContext();
            var student = new StudentProfile { Id = 1, IdentityUserId = "stud1" };
            db.StudentProfiles.Add(student);
            
            var examReleased = new Exam { Id = 1, Title = "Final", ResultsReleased = true };
            var examHidden = new Exam { Id = 2, Title = "Draft", ResultsReleased = false };
            db.Exams.AddRange(examReleased, examHidden);

            db.ExamResults.Add(new ExamResult { StudentProfileId = 1, ExamId = 1, Score = 85, Grade = "A" });
            db.ExamResults.Add(new ExamResult { StudentProfileId = 1, ExamId = 2, Score = 40, Grade = "D" });
            await db.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("stud1");

            var controller = new ExamResultsController(db, mockUserManager.Object);
            SetupController(controller, "stud1", "Student");

            var result = await controller.Index();
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<ExamResult>>(viewResult.ViewData.Model);

            Assert.Single(model);
            Assert.True(model.All(r => r.Exam!.ResultsReleased));
        }

        [Fact]
        public async Task ExamResults_Details_Student_AccessingUnreleased_ReturnsForbid()
        {
            var db = GetContext();
            var student = new StudentProfile { Id = 1, IdentityUserId = "stud1" };
            db.StudentProfiles.Add(student);
            var exam = new Exam { Id = 1, ResultsReleased = false };
            db.Exams.Add(exam);
            db.ExamResults.Add(new ExamResult { Id = 10, StudentProfileId = 1, ExamId = 1 });
            await db.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("stud1");

            var controller = new ExamResultsController(db, mockUserManager.Object);
            SetupController(controller, "stud1", "Student");

            var result = await controller.Details(10);
            Assert.IsType<ForbidResult>(result);
        }

        // --- CONTROLLER TESTS - ATTENDANCE (Faculty Logic) ---

        [Fact]
        public async Task Attendance_SaveAttendance_PersistsRecordsToDatabase()
        {
            var db = GetContext();
            db.Courses.Add(new Course { Id = 1, Name = "Logic" });
            await db.SaveChangesAsync();

            var controller = new AttendanceRecordsController(db, GetMockUserManager().Object);
            SetupController(controller, "admin1", "Admin");

            var data = new Dictionary<int, bool> { { 101, true }, { 102, false } };
            await controller.SaveAttendance(1, 1, data);

            Assert.Equal(2, await db.AttendanceRecords.CountAsync());
            Assert.Equal(1, await db.AttendanceRecords.CountAsync(a => a.IsPresent));
        }

        [Fact]
        public async Task Attendance_TakeAttendance_Faculty_CourseNotOwned_ReturnsForbid()
        {
            var db = GetContext();
            db.FacultyProfiles.Add(new FacultyProfile { Id = 5, IdentityUserId = "fac5" });
            db.Courses.Add(new Course { Id = 10, FacultyProfileId = 99 }); // Owned by another faculty
            await db.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("fac5");

            var controller = new AttendanceRecordsController(db, mockUserManager.Object);
            SetupController(controller, "fac5", "Faculty");

            var result = await controller.TakeAttendance(10);
            Assert.IsType<ForbidResult>(result);
        }

        // --- CONTROLLER TESTS - ENROLMENTS ---

        [Fact]
        public async Task CourseEnrolments_Create_Post_AddsActiveEnrolment()
        {
            var db = GetContext();
            var controller = new CourseEnrolmentsController(db, GetMockUserManager().Object);
            SetupController(controller, "admin1", "Admin");

            var enrolment = new CourseEnrolment { CourseId = 1, StudentProfileId = 1 };
            var result = await controller.Create(enrolment);

            Assert.IsType<RedirectToActionResult>(result);
            var saved = await db.CourseEnrolments.FirstOrDefaultAsync();
            Assert.NotNull(saved);
            Assert.Equal("Active", saved.Status);
        }

        // --- ADDITIONAL BUSINESS LOGIC TESTS ---

        [Fact]
        public async Task ExamResults_Details_WrongStudent_ReturnsForbid()
        {
            var db = GetContext();
            db.StudentProfiles.Add(new StudentProfile { Id = 1, IdentityUserId = "stud1" });
            db.StudentProfiles.Add(new StudentProfile { Id = 2, IdentityUserId = "stud2" });
            var exam = new Exam { Id = 1, ResultsReleased = true };
            db.Exams.Add(exam);
            db.ExamResults.Add(new ExamResult { Id = 50, StudentProfileId = 2, ExamId = 1 }); // Owned by stud2
            await db.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("stud1");

            var controller = new ExamResultsController(db, mockUserManager.Object);
            SetupController(controller, "stud1", "Student");

            var result = await controller.Details(50);
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public void GradebookService_GetGrade_HandlesScoreOverMaxScoreAsA()
        {
            // Edge case: test score higher than max (bonus points)
            Assert.Equal("A", _gradebookService.GetGrade(120, 100));
        }

        [Fact]
        public async Task Attendance_Index_Student_FiltersOnlyOwnRecords()
        {
            var db = GetContext();
            db.StudentProfiles.Add(new StudentProfile { Id = 1, IdentityUserId = "stud1" });
            db.StudentProfiles.Add(new StudentProfile { Id = 2, IdentityUserId = "stud2" });
            
            var enr1 = new CourseEnrolment { Id = 1, StudentProfileId = 1 };
            var enr2 = new CourseEnrolment { Id = 2, StudentProfileId = 2 };
            db.CourseEnrolments.AddRange(enr1, enr2);

            db.AttendanceRecords.Add(new AttendanceRecord { CourseEnrolmentId = 1, Enrolment = enr1 });
            db.AttendanceRecords.Add(new AttendanceRecord { CourseEnrolmentId = 2, Enrolment = enr2 });
            await db.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("stud1");

            var controller = new AttendanceRecordsController(db, mockUserManager.Object);
            SetupController(controller, "stud1", "Student");

            var result = await controller.Index();
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<AttendanceRecord>>(viewResult.ViewData.Model);

            Assert.Single(model);
            Assert.All(model, a => Assert.Equal(1, a.CourseEnrolmentId));
        }

        [Fact]
        public async Task Branches_Index_DisplaysAllBranches()
        {
            var db = GetContext();
            db.Branches.AddRange(new Branch { Name = "A" }, new Branch { Name = "B" });
            await db.SaveChangesAsync();

            var controller = new BranchesController(db, GetMockUserManager().Object);
            SetupController(controller, "any", "Student");

            var result = await controller.Index();
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Branch>>(viewResult.ViewData.Model);

            Assert.Equal(2, model.Count());
        }

        // --- NEW TESTS (Total 10) ---

        [Fact]
        public void GradebookService_GetGrade_Boundary70_ReturnsA()
        {
            Assert.Equal("A", _gradebookService.GetGrade(70, 100));
        }

        [Fact]
        public void GradebookService_GetGrade_Boundary69_ReturnsB()
        {
            Assert.Equal("B", _gradebookService.GetGrade(69, 100));
        }

        [Fact]
        public void GradebookService_CanFacultyAccessCourse_Owner_ReturnsTrue()
        {
            var course = new Course { FacultyProfileId = 5 };
            Assert.True(_gradebookService.CanFacultyAccessCourse(5, course));
        }

        [Fact]
        public void GradebookService_CanFacultyAccessCourse_NonOwner_ReturnsFalse()
        {
            var course = new Course { FacultyProfileId = 6 };
            Assert.False(_gradebookService.CanFacultyAccessCourse(5, course));
        }

        [Fact]
        public async Task Attendance_Details_Student_OtherRecord_ReturnsForbid()
        {
            var db = GetContext();
            db.StudentProfiles.Add(new StudentProfile { Id = 1, IdentityUserId = "stud1" });
            db.StudentProfiles.Add(new StudentProfile { Id = 2, IdentityUserId = "stud2" });
            
            var enrolment = new CourseEnrolment { Id = 1, StudentProfileId = 2 }; // Owned by stud2
            db.CourseEnrolments.Add(enrolment);
            var record = new AttendanceRecord { Id = 10, CourseEnrolmentId = 1, Enrolment = enrolment };
            db.AttendanceRecords.Add(record);
            await db.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("stud1");

            var controller = new AttendanceRecordsController(db, mockUserManager.Object);
            SetupController(controller, "stud1", "Student");

            var result = await controller.Details(10);
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task StudentProfiles_Details_Student_OtherProfile_ReturnsForbid()
        {
            var db = GetContext();
            db.StudentProfiles.Add(new StudentProfile { Id = 1, IdentityUserId = "stud1" });
            db.StudentProfiles.Add(new StudentProfile { Id = 2, IdentityUserId = "stud2" });
            await db.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("stud1");

            var controller = new StudentProfilesController(db, mockUserManager.Object);
            SetupController(controller, "stud1", "Student");

            var result = await controller.Details(2);
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task StudentProfiles_Details_Student_OwnProfile_ReturnsView()
        {
            var db = GetContext();
            db.StudentProfiles.Add(new StudentProfile { Id = 1, IdentityUserId = "stud1" });
            await db.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("stud1");

            var controller = new StudentProfilesController(db, mockUserManager.Object);
            SetupController(controller, "stud1", "Student");

            var result = await controller.Details(1);
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task StudentProfiles_Index_Student_ShowsOnlySelf()
        {
            var db = GetContext();
            db.StudentProfiles.AddRange(
                new StudentProfile { Id = 1, IdentityUserId = "stud1" },
                new StudentProfile { Id = 2, IdentityUserId = "stud2" }
            );
            await db.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("stud1");

            var controller = new StudentProfilesController(db, mockUserManager.Object);
            SetupController(controller, "stud1", "Student");

            var result = await controller.Index();
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<StudentProfile>>(viewResult.ViewData.Model);

            Assert.Single(model);
            Assert.Equal(1, model.First().Id);
        }

        [Fact]
        public async Task Attendance_TakeAttendance_Admin_ReturnsView()
        {
            var db = GetContext();
            db.Courses.Add(new Course { Id = 1, Name = "Test Course" });
            await db.SaveChangesAsync();

            var controller = new AttendanceRecordsController(db, GetMockUserManager().Object);
            SetupController(controller, "admin1", "Admin");

            var result = await controller.TakeAttendance(1);
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task CourseEnrolments_Create_Post_SetsCorrectDateAndStatus()
        {
            var db = GetContext();
            var controller = new CourseEnrolmentsController(db, GetMockUserManager().Object);
            SetupController(controller, "admin1", "Admin");

            var enrolment = new CourseEnrolment { CourseId = 1, StudentProfileId = 1 };
            await controller.Create(enrolment);

            var saved = await db.CourseEnrolments.FirstAsync();
            Assert.Equal("Active", saved.Status);
            Assert.True(DateTime.Now.Subtract(saved.EnrolDate).TotalSeconds < 5);
        }
    }
}
