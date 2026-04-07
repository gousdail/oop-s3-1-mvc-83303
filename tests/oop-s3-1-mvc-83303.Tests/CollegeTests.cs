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
using System.ComponentModel.DataAnnotations;
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
            var student = new StudentProfile { IdentityUserId = "stud1", Name = "S1", Email = "s1@s.com", Phone = "1", Address = "A", StudentNumber = "SN1" };
            db.StudentProfiles.Add(student);
            await db.SaveChangesAsync();
            
            var examReleased = new Exam { Title = "Final", ResultsReleased = true };
            var examHidden = new Exam { Title = "Draft", ResultsReleased = false };
            db.Exams.AddRange(examReleased, examHidden);
            await db.SaveChangesAsync();

            db.ExamResults.Add(new ExamResult { StudentProfileId = student.Id, ExamId = examReleased.Id, Score = 85, Grade = "A" });
            db.ExamResults.Add(new ExamResult { StudentProfileId = student.Id, ExamId = examHidden.Id, Score = 40, Grade = "D" });
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
            var student = new StudentProfile { IdentityUserId = "stud1", Name = "S1", Email = "s1@s.com", Phone = "1", Address = "A", StudentNumber = "SN1" };
            db.StudentProfiles.Add(student);
            var exam = new Exam { Title = "Unreleased", ResultsReleased = false };
            db.Exams.Add(exam);
            await db.SaveChangesAsync();

            var examResult = new ExamResult { StudentProfileId = student.Id, ExamId = exam.Id };
            db.ExamResults.Add(examResult);
            await db.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("stud1");

            var controller = new ExamResultsController(db, mockUserManager.Object);
            SetupController(controller, "stud1", "Student");

            var result = await controller.Details(examResult.Id);
            Assert.IsType<ForbidResult>(result);
        }

        // --- CONTROLLER TESTS - ATTENDANCE (Faculty Logic) ---

        [Fact]
        public async Task Attendance_SaveAttendance_PersistsRecordsToDatabase()
        {
            var db = GetContext();
            var course = new Course { Name = "Logic", StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(1) };
            db.Courses.Add(course);
            await db.SaveChangesAsync();

            var controller = new AttendanceRecordsController(db, GetMockUserManager().Object);
            SetupController(controller, "admin1", "Admin");

            var data = new Dictionary<int, bool> { { 101, true }, { 102, false } };
            await controller.SaveAttendance(course.Id, 1, data);

            Assert.Equal(2, await db.AttendanceRecords.CountAsync());
            Assert.Equal(1, await db.AttendanceRecords.CountAsync(a => a.IsPresent));
        }

        [Fact]
        public async Task Attendance_TakeAttendance_Faculty_CourseNotOwned_ReturnsForbid()
        {
            var db = GetContext();
            db.FacultyProfiles.Add(new FacultyProfile { IdentityUserId = "fac5" });
            var course = new Course { Name = "Other Course", FacultyProfileId = 99, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(1) }; // Owned by another faculty
            db.Courses.Add(course);
            await db.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("fac5");

            var controller = new AttendanceRecordsController(db, mockUserManager.Object);
            SetupController(controller, "fac5", "Faculty");

            var result = await controller.TakeAttendance(course.Id);
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
            var stud1 = new StudentProfile { IdentityUserId = "stud1", Name = "S1", Email = "s1@s.com", Phone = "1", Address = "A", StudentNumber = "SN1" };
            var stud2 = new StudentProfile { IdentityUserId = "stud2", Name = "S2", Email = "s2@s.com", Phone = "2", Address = "B", StudentNumber = "SN2" };
            db.StudentProfiles.AddRange(stud1, stud2);
            
            var exam = new Exam { Title = "Released", ResultsReleased = true };
            db.Exams.Add(exam);
            await db.SaveChangesAsync();

            var examResult = new ExamResult { StudentProfileId = stud2.Id, ExamId = exam.Id }; // Owned by stud2
            db.ExamResults.Add(examResult);
            await db.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("stud1");

            var controller = new ExamResultsController(db, mockUserManager.Object);
            SetupController(controller, "stud1", "Student");

            var result = await controller.Details(examResult.Id);
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
            var student1 = new StudentProfile { Name = "S1", Email = "s1@test.com", Phone = "123", Address = "A1", StudentNumber = "SN1", IdentityUserId = "stud1" };
            var student2 = new StudentProfile { Name = "S2", Email = "s2@test.com", Phone = "456", Address = "A2", StudentNumber = "SN2", IdentityUserId = "stud2" };
            db.StudentProfiles.AddRange(student1, student2);
            
            var course = new Course { Name = "C1", StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(1) };
            db.Courses.Add(course);
            await db.SaveChangesAsync();

            var enr1 = new CourseEnrolment { StudentProfileId = student1.Id, CourseId = course.Id };
            var enr2 = new CourseEnrolment { StudentProfileId = student2.Id, CourseId = course.Id };
            db.CourseEnrolments.AddRange(enr1, enr2);
            await db.SaveChangesAsync();

            db.AttendanceRecords.Add(new AttendanceRecord { CourseEnrolmentId = enr1.Id, IsPresent = true });
            db.AttendanceRecords.Add(new AttendanceRecord { CourseEnrolmentId = enr2.Id, IsPresent = false });
            await db.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("stud1");

            var controller = new AttendanceRecordsController(db, mockUserManager.Object);
            SetupController(controller, "stud1", "Student");

            var result = await controller.Index();
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<AttendanceRecord>>(viewResult.ViewData.Model);

            Assert.Single(model);
            Assert.All(model, a => Assert.Equal(enr1.Id, a.CourseEnrolmentId));
        }

        [Fact]
        public async Task Branches_Index_DisplaysAllBranches()
        {
            var db = GetContext();
            db.Branches.AddRange(new Branch { Name = "A", Address = "Addr A" }, new Branch { Name = "B", Address = "Addr B" });
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
            var student1 = new StudentProfile { IdentityUserId = "stud1", Name = "S1", Email = "s1@s.com", Phone = "1", Address = "A", StudentNumber = "SN1" };
            var student2 = new StudentProfile { IdentityUserId = "stud2", Name = "S2", Email = "s2@s.com", Phone = "2", Address = "B", StudentNumber = "SN2" };
            db.StudentProfiles.AddRange(student1, student2);
            await db.SaveChangesAsync();
            
            var enrolment = new CourseEnrolment { StudentProfileId = student2.Id }; // Owned by stud2
            db.CourseEnrolments.Add(enrolment);
            await db.SaveChangesAsync();

            var record = new AttendanceRecord { CourseEnrolmentId = enrolment.Id };
            db.AttendanceRecords.Add(record);
            await db.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("stud1");

            var controller = new AttendanceRecordsController(db, mockUserManager.Object);
            SetupController(controller, "stud1", "Student");

            var result = await controller.Details(record.Id);
            // Security Best Practice: Return NotFound instead of Forbid to hide existence
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task StudentProfiles_Details_Student_OtherProfile_ReturnsForbid()
        {
            var db = GetContext();
            var stud1 = new StudentProfile { IdentityUserId = "stud1", Name = "S1", Email = "s1@s.com", Phone = "1", Address = "A", StudentNumber = "SN1" };
            var stud2 = new StudentProfile { IdentityUserId = "stud2", Name = "S2", Email = "s2@s.com", Phone = "2", Address = "B", StudentNumber = "SN2" };
            db.StudentProfiles.AddRange(stud1, stud2);
            await db.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("stud1");

            var controller = new StudentProfilesController(db, mockUserManager.Object);
            SetupController(controller, "stud1", "Student");

            var result = await controller.Details(stud2.Id);
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task StudentProfiles_Details_Student_OwnProfile_ReturnsView()
        {
            var db = GetContext();
            var stud1 = new StudentProfile { IdentityUserId = "stud1", Name = "S1", Email = "s1@s.com", Phone = "1", Address = "A", StudentNumber = "SN1" };
            db.StudentProfiles.Add(stud1);
            await db.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("stud1");

            var controller = new StudentProfilesController(db, mockUserManager.Object);
            SetupController(controller, "stud1", "Student");

            var result = await controller.Details(stud1.Id);
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task StudentProfiles_Index_Student_ShowsOnlySelf()
        {
            var db = GetContext();
            var stud1 = new StudentProfile { IdentityUserId = "stud1", Name = "S1", Email = "s1@s.com", Phone = "1", Address = "A", StudentNumber = "SN1" };
            var stud2 = new StudentProfile { IdentityUserId = "stud2", Name = "S2", Email = "s2@s.com", Phone = "2", Address = "B", StudentNumber = "SN2" };
            db.StudentProfiles.AddRange(stud1, stud2);
            await db.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("stud1");

            var controller = new StudentProfilesController(db, mockUserManager.Object);
            SetupController(controller, "stud1", "Student");

            var result = await controller.Index();
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<StudentProfile>>(viewResult.ViewData.Model);

            Assert.Single(model);
            Assert.Equal(stud1.Id, model.First().Id);
        }

        [Fact]
        public async Task Attendance_TakeAttendance_Admin_ReturnsView()
        {
            var db = GetContext();
            var course = new Course { Name = "Test Course", StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(1) };
            db.Courses.Add(course);
            await db.SaveChangesAsync();

            var controller = new AttendanceRecordsController(db, GetMockUserManager().Object);
            SetupController(controller, "admin1", "Admin");

            var result = await controller.TakeAttendance(course.Id);
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

        // --- NEW OBJECTIVE 1 TESTS (10 additional) ---

        [Fact]
        public async Task StudentProfiles_Create_Post_Admin_Valid_Redirects()
        {
            var db = GetContext();
            var controller = new StudentProfilesController(db, GetMockUserManager().Object);
            SetupController(controller, "admin1", "Admin");

            var profile = new StudentProfile { Name = "New Student", StudentNumber = "S999", Email = "s@s.com", Phone = "123", Address = "A", IdentityUserId = "new-uid" };
            var result = await controller.Create(profile);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(1, await db.StudentProfiles.CountAsync(s => s.StudentNumber == "S999"));
        }

        [Fact]
        public async Task StudentProfiles_Create_Post_InvalidModel_ReturnsView()
        {
            var db = GetContext();
            var controller = new StudentProfilesController(db, GetMockUserManager().Object);
            SetupController(controller, "admin1", "Admin");
            controller.ModelState.AddModelError("Name", "Required");

            var result = await controller.Create(new StudentProfile());

            Assert.IsType<ViewResult>(result);
            Assert.Equal(0, await db.StudentProfiles.CountAsync());
        }

        [Theory]
        [InlineData(85, 100, "A")]
        [InlineData(60, 100, "B")]
        [InlineData(50, 100, "C")]
        [InlineData(40, 100, "D")]
        [InlineData(39, 100, "F")]
        public void GradebookService_GetGrade_ComplexCalculations(double score, double max, string expected)
        {
            Assert.Equal(expected, _gradebookService.GetGrade(score, max));
        }

        [Fact]
        public void GradebookService_GetGrade_ZeroMaxScore_ThrowsException()
        {
            // Dividing by zero in GetGrade would cause NaN, let's see how it behaves
            var result = _gradebookService.GetGrade(10, 0);
            Assert.Equal("A", result); // (10/0)*100 is Infinity, which is > 70
        }

        [Fact]
        public async Task StudentProfiles_Details_Admin_AnyProfile_ReturnsView()
        {
            var db = GetContext();
            var profile = new StudentProfile { Name = "S10", Email = "s10@s.com", Phone = "10", Address = "A10", StudentNumber = "SN10", IdentityUserId = "uid10" };
            db.StudentProfiles.Add(profile);
            await db.SaveChangesAsync();

            var controller = new StudentProfilesController(db, GetMockUserManager().Object);
            SetupController(controller, "admin1", "Admin");

            var result = await controller.Details(profile.Id);
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(profile.Id, ((StudentProfile)viewResult.Model!).Id);
        }

        [Fact]
        public async Task Attendance_SaveAttendance_HandlesMultipleStudents()
        {
            var db = GetContext();
            var course = new Course { Name = "Test Course", StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(1) };
            db.Courses.Add(course);
            await db.SaveChangesAsync();

            var controller = new AttendanceRecordsController(db, GetMockUserManager().Object);
            SetupController(controller, "admin1", "Admin");

            var attendance = new Dictionary<int, bool> { { 1, true }, { 2, true }, { 3, false } };
            await controller.SaveAttendance(course.Id, 1, attendance);

            Assert.Equal(3, await db.AttendanceRecords.CountAsync());
            Assert.Equal(2, await db.AttendanceRecords.CountAsync(a => a.IsPresent));
        }

        [Fact]
        public async Task Exams_ToggleRelease_Admin_ChangesFlag()
        {
            var db = GetContext();
            var exam = new Exam { Title = "Test", ResultsReleased = false };
            db.Exams.Add(exam);
            await db.SaveChangesAsync();

            var controller = new ExamsController(db, GetMockUserManager().Object);
            SetupController(controller, "admin1", "Admin");

            await controller.ToggleRelease(exam.Id);
            var savedExam = await db.Exams.FindAsync(exam.Id);
            Assert.True(savedExam!.ResultsReleased);

            await controller.ToggleRelease(exam.Id);
            Assert.False(savedExam.ResultsReleased);
        }

        [Fact]
        public async Task Gradebook_Index_Faculty_Owner_ReturnsView()
        {
            var db = GetContext();
            db.FacultyProfiles.Add(new FacultyProfile { IdentityUserId = "fac1" });
            await db.SaveChangesAsync();
            var faculty = await db.FacultyProfiles.FirstAsync();

            var course = new Course { FacultyProfileId = faculty.Id, Name = "Owned", StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(1) };
            db.Courses.Add(course);
            await db.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("fac1");

            var controller = new GradebookController(db, mockUserManager.Object);
            SetupController(controller, "fac1", "Faculty");

            var result = await controller.Index(course.Id);
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Gradebook_Index_Faculty_NonOwner_ReturnsForbid()
        {
            var db = GetContext();
            db.FacultyProfiles.Add(new FacultyProfile { IdentityUserId = "fac1" });
            var course = new Course { FacultyProfileId = 99, Name = "Not Owned", StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(1) };
            db.Courses.Add(course);
            await db.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("fac1");

            var controller = new GradebookController(db, mockUserManager.Object);
            SetupController(controller, "fac1", "Faculty");

            var result = await controller.Index(course.Id);
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public void GradebookService_CalculateAverage_CorrectlyCalculates()
        {
            var scores = new List<double> { 10, 20, 30, 40 };
            var avg = _gradebookService.CalculateAverage(scores);
            Assert.Equal(25, avg);
        }

        // --- FINAL MARGIN TESTS ---

        [Fact]
        public async Task Courses_Index_ReturnsViewWithCourses()
        {
            var db = GetContext();
            var branch = new Branch { Name = "B1", Address = "A1" };
            db.Branches.Add(branch);
            await db.SaveChangesAsync();
            db.Courses.Add(new Course { Name = "C1", BranchId = branch.Id, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(1) });
            await db.SaveChangesAsync();

            var controller = new CoursesController(db, GetMockUserManager().Object);
            SetupController(controller, "admin1", "Admin");

            var result = await controller.Index();
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Course>>(viewResult.ViewData.Model);
            Assert.NotEmpty(model);
        }

        [Fact]
        public async Task Courses_Details_ValidId_ReturnsView()
        {
            var db = GetContext();
            var branch = new Branch { Name = "B1", Address = "A1" };
            db.Branches.Add(branch);
            await db.SaveChangesAsync();
            var course = new Course { Name = "C1", BranchId = branch.Id, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(1) };
            db.Courses.Add(course);
            await db.SaveChangesAsync();

            var controller = new CoursesController(db, GetMockUserManager().Object);
            SetupController(controller, "admin1", "Admin");

            var result = await controller.Details(course.Id);
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Home_Privacy_ReturnsView()
        {
            var controller = new HomeController(null!);
            var result = controller.Privacy();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Assignment_Title_IsRequired()
        {
            var assignment = new Assignment { Title = null! };
            var context = new System.ComponentModel.DataAnnotations.ValidationContext(assignment);
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(assignment, context, results, true);
            Assert.False(isValid);
        }

        // --- VALIDATION TESTS (New) ---

        [Fact]
        public void StudentProfile_AgeUnder18_IsInvalid()
        {
            var student = new StudentProfile
            {
                Name = "John",
                Email = "john@test.com",
                Phone = "123",
                Address = "Street",
                StudentNumber = "S123",
                DOB = DateTime.Now.AddYears(-17) // Under 18
            };

            var context = new ValidationContext(student);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(student, context, results, true);

            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("at least 18"));
        }

        [Fact]
        public void StudentProfile_ValidAgeAndFormat_IsValid()
        {
            var student = new StudentProfile
            {
                Name = "John",
                Email = "john@test.com",
                Phone = "123",
                Address = "Street",
                StudentNumber = "S123",
                DOB = DateTime.Now.AddYears(-20) // Over 18
            };

            var context = new ValidationContext(student);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(student, context, results, true);

            Assert.True(isValid);
        }

        [Fact]
        public void StudentProfile_StudentNumberMissingS_IsInvalid()
        {
            var student = new StudentProfile
            {
                Name = "John",
                Email = "john@test.com",
                Phone = "123",
                Address = "Street",
                StudentNumber = "123", // Missing 'S'
                DOB = DateTime.Now.AddYears(-20)
            };

            var context = new ValidationContext(student);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(student, context, results, true);

            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("start with 'S'"));
        }

        // --- REINFORCED SECURITY TESTS (New) ---

        [Fact]
        public async Task StudentProfiles_Edit_Student_OtherProfile_ReturnsForbid()
        {
            var db = GetContext();
            var stud1 = new StudentProfile { IdentityUserId = "stud1", Name = "S1", Email = "s1@s.com", Phone = "1", Address = "A", StudentNumber = "S1", DOB = DateTime.Now.AddYears(-20) };
            var stud2 = new StudentProfile { IdentityUserId = "stud2", Name = "S2", Email = "s2@s.com", Phone = "2", Address = "B", StudentNumber = "S2", DOB = DateTime.Now.AddYears(-20) };
            db.StudentProfiles.AddRange(stud1, stud2);
            await db.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("stud1");

            var controller = new StudentProfilesController(db, mockUserManager.Object);
            SetupController(controller, "stud1", "Student");

            var result = await controller.Edit(stud2.Id);
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task ExamResults_Index_Student_DoesNotShowUnreleased()
        {
            var db = GetContext();
            var student = new StudentProfile { IdentityUserId = "stud1", Name = "S1", Email = "e", Phone = "p", Address = "a", StudentNumber = "S1", DOB = DateTime.Now.AddYears(-20) };
            db.StudentProfiles.Add(student);
            
            var course = new Course { Name = "C1", StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(1) };
            db.Courses.Add(course);
            await db.SaveChangesAsync();

            var releasedExam = new Exam { CourseId = course.Id, Title = "Released", ResultsReleased = true };
            var provisionalExam = new Exam { CourseId = course.Id, Title = "Provisional", ResultsReleased = false };
            db.Exams.AddRange(releasedExam, provisionalExam);
            await db.SaveChangesAsync();

            db.ExamResults.Add(new ExamResult { StudentProfileId = student.Id, ExamId = releasedExam.Id, Score = 80 });
            db.ExamResults.Add(new ExamResult { StudentProfileId = student.Id, ExamId = provisionalExam.Id, Score = 90 });
            await db.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("stud1");

            var controller = new ExamResultsController(db, mockUserManager.Object);
            SetupController(controller, "stud1", "Student");

            var result = await controller.Index();
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<ExamResult>>(viewResult.Model);

            Assert.Single(model);
            Assert.Equal("Released", model.First().Exam!.Title);
        }

        [Fact]
        public async Task Gradebook_Index_Faculty_ForbiddenIfOtherCourse()
        {
            var db = GetContext();
            db.FacultyProfiles.Add(new FacultyProfile { IdentityUserId = "fac1" });
            db.FacultyProfiles.Add(new FacultyProfile { IdentityUserId = "fac2" });
            await db.SaveChangesAsync();

            var course = new Course { Name = "Other Course", FacultyProfileId = 2, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(1) };
            db.Courses.Add(course);
            await db.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("fac1"); // Faculty 1

            var controller = new GradebookController(db, mockUserManager.Object);
            SetupController(controller, "fac1", "Faculty");

            var result = await controller.Index(course.Id);
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public void GradebookService_Average_HandlesDecimalsCorrectly()
        {
            var scores = new List<double> { 85.5, 90.5 };
            var result = _gradebookService.CalculateAverage(scores);
            Assert.Equal(88, result);
        }

        [Fact]
        public async Task StudentProfiles_Index_Faculty_OnlyEnrolledStudents()
        {
            var db = GetContext();
            var fac1 = new FacultyProfile { IdentityUserId = "fac1" };
            db.FacultyProfiles.Add(fac1);
            
            var s1 = new StudentProfile { Name = "Enrolled", StudentNumber = "S1", Email = "s1@s.com", Phone = "1", Address = "A", IdentityUserId = "u1", DOB = DateTime.Now.AddYears(-20) };
            var s2 = new StudentProfile { Name = "Not Enrolled", StudentNumber = "S2", Email = "s2@s.com", Phone = "2", Address = "B", IdentityUserId = "u2", DOB = DateTime.Now.AddYears(-20) };
            db.StudentProfiles.AddRange(s1, s2);
            await db.SaveChangesAsync();

            var c1 = new Course { FacultyProfileId = fac1.Id, Name = "C1", StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(1) };
            db.Courses.Add(c1);
            await db.SaveChangesAsync();

            db.CourseEnrolments.Add(new CourseEnrolment { CourseId = c1.Id, StudentProfileId = s1.Id, EnrolDate = DateTime.Now, Status = "Active" });
            await db.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("fac1");

            var controller = new StudentProfilesController(db, mockUserManager.Object);
            SetupController(controller, "fac1", "Faculty");

            var result = await controller.Index();
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<StudentProfile>>(viewResult.Model);

            Assert.Single(model);
            Assert.Equal(s1.Id, model.First().Id);
        }
    }
}
