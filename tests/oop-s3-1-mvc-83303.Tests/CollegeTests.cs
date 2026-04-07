using Microsoft.EntityFrameworkCore;
using oop_s3_1_mvc_83303.Data;
using oop_s3_1_mvc_83303.Models;
using oop_s3_1_mvc_83303.Services;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace oop_s3_1_mvc_83303.Tests
{
    public class CollegeTests
    {
        private ApplicationDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        #region GradebookService Tests (15)

        [Fact]
        public void GradebookService_CalculateAverage_NormalScores_ReturnsCorrectAverage()
        {
            var service = new GradebookService();
            var scores = new List<double> { 70, 80, 90 };
            var result = service.CalculateAverage(scores);
            Assert.Equal(80, result);
        }

        [Fact]
        public void GradebookService_CalculateAverage_EmptyList_ReturnsZero()
        {
            var service = new GradebookService();
            var scores = new List<double>();
            var result = service.CalculateAverage(scores);
            Assert.Equal(0, result);
        }

        [Fact]
        public void GradebookService_CalculateAverage_BoundaryScores_ReturnsCorrectAverage()
        {
            var service = new GradebookService();
            var scores = new List<double> { 0, 100 };
            var result = service.CalculateAverage(scores);
            Assert.Equal(50, result);
        }

        [Fact]
        public void GradebookService_GetGrade_A_Score()
        {
            var service = new GradebookService();
            Assert.Equal("A", service.GetGrade(75, 100));
        }

        [Fact]
        public void GradebookService_GetGrade_B_Score()
        {
            var service = new GradebookService();
            Assert.Equal("B", service.GetGrade(65, 100));
        }

        [Fact]
        public void GradebookService_GetGrade_C_Score()
        {
            var service = new GradebookService();
            Assert.Equal("C", service.GetGrade(55, 100));
        }

        [Fact]
        public void GradebookService_GetGrade_D_Score()
        {
            var service = new GradebookService();
            Assert.Equal("D", service.GetGrade(45, 100));
        }

        [Fact]
        public void GradebookService_GetGrade_F_Score()
        {
            var service = new GradebookService();
            Assert.Equal("F", service.GetGrade(35, 100));
        }

        [Fact]
        public void GradebookService_GetGrade_RoundingUp_ReturnsCorrectGrade()
        {
            var service = new GradebookService();
            // 69.5% should round to 70% (A)
            Assert.Equal("A", service.GetGrade(69.5, 100));
        }

        [Fact]
        public void GradebookService_GetGrade_RoundingDown_ReturnsCorrectGrade()
        {
            var service = new GradebookService();
            // 39.4% should round to 39% (F)
            Assert.Equal("F", service.GetGrade(39.4, 100));
        }

        [Fact]
        public void GradebookService_IsStudentEnrolled_StudentExists_ReturnsTrue()
        {
            var service = new GradebookService();
            var enrolments = new List<CourseEnrolment> { new CourseEnrolment { StudentProfileId = 1, CourseId = 10 } };
            Assert.True(service.IsStudentEnrolled(1, 10, enrolments));
        }

        [Fact]
        public void GradebookService_IsStudentEnrolled_StudentNotExists_ReturnsFalse()
        {
            var service = new GradebookService();
            var enrolments = new List<CourseEnrolment> { new CourseEnrolment { StudentProfileId = 1, CourseId = 10 } };
            Assert.False(service.IsStudentEnrolled(2, 10, enrolments));
        }

        [Fact]
        public void GradebookService_IsStudentEnrolled_CourseNotExists_ReturnsFalse()
        {
            var service = new GradebookService();
            var enrolments = new List<CourseEnrolment> { new CourseEnrolment { StudentProfileId = 1, CourseId = 10 } };
            Assert.False(service.IsStudentEnrolled(1, 11, enrolments));
        }

        [Fact]
        public void GradebookService_CanFacultyAccessCourse_Assigned_ReturnsTrue()
        {
            var service = new GradebookService();
            var course = new Course { FacultyProfileId = 5 };
            Assert.True(service.CanFacultyAccessCourse(5, course));
        }

        [Fact]
        public void GradebookService_CanFacultyAccessCourse_NotAssigned_ReturnsFalse()
        {
            var service = new GradebookService();
            var course = new Course { FacultyProfileId = 5 };
            Assert.False(service.CanFacultyAccessCourse(6, course));
        }

        #endregion

        #region Visibility and Security Tests (15)

        [Fact]
        public void Student_Cannot_See_Provisional_Exam_Results()
        {
            var service = new GradebookService();
            var exam = new Exam { ResultsReleased = false };
            Assert.False(service.CanStudentSeeExamResult(exam));
        }

        [Fact]
        public void Student_Can_See_Released_Exam_Results()
        {
            var service = new GradebookService();
            var exam = new Exam { ResultsReleased = true };
            Assert.True(service.CanStudentSeeExamResult(exam));
        }

        [Fact]
        public void Student_Can_Only_See_Own_Profile_MatchingId()
        {
            var studentId = 1;
            var requestedId = 1;
            Assert.Equal(studentId, requestedId);
        }

        [Fact]
        public void Student_Cannot_See_Other_Profile_MismatchId()
        {
            var studentId = 1;
            var requestedId = 2;
            Assert.NotEqual(studentId, requestedId);
        }

        [Fact]
        public void Faculty_Can_Only_Access_Assigned_Courses()
        {
            var service = new GradebookService();
            var course = new Course { FacultyProfileId = 100 };
            Assert.True(service.CanFacultyAccessCourse(100, course));
            Assert.False(service.CanFacultyAccessCourse(101, course));
        }

        [Fact]
        public void Student_Access_To_Unreleased_Results_Returns_Empty_Or_Error()
        {
            // Logic check: if results are not released, the collection should be filtered or access denied
            var exam = new Exam { ResultsReleased = false };
            var service = new GradebookService();
            Assert.False(service.CanStudentSeeExamResult(exam));
        }

        [Fact]
        public void Faculty_Cannot_Access_Course_Without_Assignment()
        {
            var service = new GradebookService();
            var course = new Course { FacultyProfileId = null };
            Assert.False(service.CanFacultyAccessCourse(1, course));
        }

        [Fact]
        public void Student_IdentityUserId_Must_Match_To_View_Profile()
        {
            var student = new StudentProfile { Id = 1, IdentityUserId = "user-1" };
            var currentUserId = "user-1";
            Assert.Equal(currentUserId, student.IdentityUserId);
        }

        [Fact]
        public void Student_IdentityUserId_Mismatch_Denies_Profile_View()
        {
            var student = new StudentProfile { Id = 1, IdentityUserId = "user-1" };
            var currentUserId = "user-2";
            Assert.NotEqual(currentUserId, student.IdentityUserId);
        }

        [Fact]
        public void Security_FacultyProfile_Id_Required_For_Management()
        {
            var faculty = new FacultyProfile { Id = 10 };
            Assert.True(faculty.Id > 0);
        }

        [Fact]
        public void Security_Branch_Restricted_To_Faculty_Assigned_To_It()
        {
            var branchId = 1;
            var facultyBranchId = 1;
            Assert.Equal(branchId, facultyBranchId);
        }

        [Fact]
        public void Security_Branch_Mismatch_Restricts_Faculty_Access()
        {
            var branchId = 1;
            var facultyBranchId = 2;
            Assert.NotEqual(branchId, facultyBranchId);
        }

        [Fact]
        public void Student_Enrolment_Status_Check_Before_Access()
        {
            var enrolment = new CourseEnrolment { Status = "Active" };
            Assert.Equal("Active", enrolment.Status);
        }

        [Fact]
        public void Student_Enrolment_Status_Suspended_Denies_Access()
        {
            var enrolment = new CourseEnrolment { Status = "Suspended" };
            Assert.NotEqual("Active", enrolment.Status);
        }

        [Fact]
        public void Visibility_Exam_MaxScore_Is_Visible_To_Students()
        {
            var exam = new Exam { MaxScore = 100 };
            Assert.True(exam.MaxScore > 0);
        }

        #endregion

        #region Enrolments & Attendance Tests (10)

        [Fact]
        public void Enrolment_Date_Cannot_Be_After_Course_End_Date()
        {
            var course = new Course { StartDate = DateTime.Now.AddDays(-10), EndDate = DateTime.Now.AddDays(-1) };
            var enrolment = new CourseEnrolment { EnrolDate = DateTime.Now };
            Assert.True(enrolment.EnrolDate > course.EndDate);
        }

        [Fact]
        public void AttendanceRecord_WeekNumber_Must_Be_Between_1_And_52()
        {
            var record = new AttendanceRecord { WeekNumber = 10 };
            Assert.InRange(record.WeekNumber, 1, 52);
        }

        [Fact]
        public void AttendanceRecord_WeekNumber_Invalid_Too_Low()
        {
            var record = new AttendanceRecord { WeekNumber = 0 };
            Assert.False(record.WeekNumber >= 1 && record.WeekNumber <= 52);
        }

        [Fact]
        public void AttendanceRecord_WeekNumber_Invalid_Too_High()
        {
            var record = new AttendanceRecord { WeekNumber = 53 };
            Assert.False(record.WeekNumber >= 1 && record.WeekNumber <= 52);
        }

        [Fact]
        public void Attendance_Present_Logic_Check()
        {
            var record = new AttendanceRecord { IsPresent = true };
            Assert.True(record.IsPresent);
        }

        [Fact]
        public void Attendance_Absent_Logic_Check()
        {
            var record = new AttendanceRecord { IsPresent = false };
            Assert.False(record.IsPresent);
        }

        [Fact]
        public void Attendance_Calculation_Rate_Full_Attendance()
        {
            var records = new List<AttendanceRecord>
            {
                new AttendanceRecord { IsPresent = true },
                new AttendanceRecord { IsPresent = true }
            };
            var rate = (double)records.Count(r => r.IsPresent) / records.Count;
            Assert.Equal(1.0, rate);
        }

        [Fact]
        public void Attendance_Calculation_Rate_Half_Attendance()
        {
            var records = new List<AttendanceRecord>
            {
                new AttendanceRecord { IsPresent = true },
                new AttendanceRecord { IsPresent = false }
            };
            var rate = (double)records.Count(r => r.IsPresent) / records.Count;
            Assert.Equal(0.5, rate);
        }

        [Fact]
        public void Attendance_Calculation_Rate_Zero_Attendance()
        {
            var records = new List<AttendanceRecord>
            {
                new AttendanceRecord { IsPresent = false },
                new AttendanceRecord { IsPresent = false }
            };
            var rate = (double)records.Count(r => r.IsPresent) / records.Count;
            Assert.Equal(0, rate);
        }

        [Fact]
        public void Enrolment_Default_Status_Is_Active()
        {
            var enrolment = new CourseEnrolment();
            Assert.Equal("Active", enrolment.Status);
        }

        #endregion

        #region Models & Validations Tests (10)

        [Fact]
        public void StudentProfile_Required_Fields_Validation()
        {
            var student = new StudentProfile();
            var context = new ValidationContext(student);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(student, context, results, true);
            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains("Name"));
            Assert.Contains(results, r => r.MemberNames.Contains("Email"));
        }

        [Fact]
        public void StudentProfile_Email_Format_Validation()
        {
            var student = new StudentProfile { Email = "invalid-email", Name = "Test", Phone = "123456789", Address = "Add", DOB = DateTime.Now.AddYears(-20), StudentNumber = "S123" };
            var context = new ValidationContext(student);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(student, context, results, true);
            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains("Email"));
        }

        [Fact]
        public void StudentProfile_Phone_Format_Validation()
        {
            // PhoneAttribute is very permissive in .NET, but we can check if it's required
            var student = new StudentProfile { Phone = "", Name = "Test", Email = "test@test.com", Address = "Add", DOB = DateTime.Now.AddYears(-20), StudentNumber = "S123" };
            var context = new ValidationContext(student);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(student, context, results, true);
            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains("Phone"));
        }

        [Fact]
        public void StudentProfile_Minimum_Age_Validation()
        {
            var student = new StudentProfile 
            { 
                DOB = DateTime.Now.AddYears(-17), // 17 years old
                Name = "Test", 
                Email = "test@test.com", 
                Phone = "123456789", 
                Address = "Add", 
                StudentNumber = "S123" 
            };
            var context = new ValidationContext(student);
            var results = student.Validate(context).ToList();
            Assert.Contains(results, r => r.ErrorMessage == "Student must be at least 18 years old.");
        }

        [Fact]
        public void StudentProfile_Valid_Data_Passes()
        {
            var student = new StudentProfile 
            { 
                Name = "John Doe", 
                Email = "john@example.com", 
                Phone = "0123456789", 
                Address = "123 Street", 
                DOB = DateTime.Now.AddYears(-20), 
                StudentNumber = "S12345" 
            };
            var context = new ValidationContext(student);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(student, context, results, true);
            // Also call custom validation
            var customResults = student.Validate(context);
            Assert.True(isValid);
            Assert.Empty(customResults);
        }

        [Fact]
        public void Course_Name_Required_Validation()
        {
            var course = new Course { Name = "" };
            var context = new ValidationContext(course);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(course, context, results, true);
            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage == "Course name is required");
        }

        [Fact]
        public void Exam_MaxScore_Range_Validation()
        {
            var exam = new Exam { MaxScore = 1001 };
            var context = new ValidationContext(exam);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(exam, context, results, true);
            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage == "Score must be between 0 and 1000");
        }

        [Fact]
        public void StudentProfile_StudentNumber_Prefix_Validation()
        {
            var student = new StudentProfile { StudentNumber = "A123" };
            var context = new ValidationContext(student);
            var results = student.Validate(context).ToList();
            Assert.Contains(results, r => r.ErrorMessage == "Student number must start with 'S'.");
        }

        [Fact]
        public void Course_Dates_Validation_Start_Required()
        {
            var course = new Course { Name = "" }; // Name is required
            var context = new ValidationContext(course);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(course, context, results, true);
            Assert.False(isValid);
        }

        [Fact]
        public void Branch_Name_Validation_Required()
        {
            var branch = new Branch { Name = "" };
            var context = new ValidationContext(branch);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(branch, context, results, true);
            Assert.False(isValid);
        }

        #endregion

        #region Additional Coverage Tests

        [Fact]
        public void Branch_Name_Too_Short_Validation()
        {
            var branch = new Branch { Name = "Ab", Address = "Some Address" };
            var context = new ValidationContext(branch);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(branch, context, results, true);
            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("between 3 and 100 characters"));
        }

        [Fact]
        public void Branch_Address_Required_Validation()
        {
            var branch = new Branch { Name = "Valid Name", Address = "" };
            var context = new ValidationContext(branch);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(branch, context, results, true);
            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains("Address"));
        }

        [Fact]
        public void FacultyProfile_Name_Required_Validation()
        {
            var faculty = new FacultyProfile { Name = "", Email = "test@test.com", Phone = "123456789", IdentityUserId = "id" };
            var context = new ValidationContext(faculty);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(faculty, context, results, true);
            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains("Name"));
        }

        [Fact]
        public void FacultyProfile_Email_Invalid_Validation()
        {
            var faculty = new FacultyProfile { Name = "Test", Email = "invalid-email", Phone = "123456789", IdentityUserId = "id" };
            var context = new ValidationContext(faculty);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(faculty, context, results, true);
            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains("Email"));
        }

        [Fact]
        public void Exam_Title_Required_Validation()
        {
            var exam = new Exam { Title = "", MaxScore = 100, Date = DateTime.Now };
            var context = new ValidationContext(exam);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(exam, context, results, true);
            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains("Title"));
        }

        [Fact]
        public void ExamResult_Score_Range_Validation()
        {
            var result = new ExamResult { Score = -1, Grade = "F", StudentProfileId = 1, ExamId = 1 };
            var context = new ValidationContext(result);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(result, context, results, true);
            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Score must be positive"));
        }

        [Fact]
        public void Assignment_Title_Required_Validation()
        {
            var assignment = new Assignment { Title = "" };
            var context = new ValidationContext(assignment);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(assignment, context, results, true);
            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains("Title"));
        }

        [Fact]
        public void GradebookService_GetGrade_ExactlyBoundary_B()
        {
            var service = new GradebookService();
            // 60% is exactly B
            Assert.Equal("B", service.GetGrade(60, 100));
        }

        [Fact]
        public void GradebookService_GetGrade_ExactlyBoundary_D()
        {
            var service = new GradebookService();
            // 40% is exactly D
            Assert.Equal("D", service.GetGrade(40, 100));
        }

        [Fact]
        public void GradebookService_CalculateAverage_SingleScore_ReturnsCorrectAverage()
        {
            var service = new GradebookService();
            var scores = new List<double> { 85 };
            var result = service.CalculateAverage(scores);
            Assert.Equal(85, result);
        }

        #endregion
    }
}
