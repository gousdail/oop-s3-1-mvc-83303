using Microsoft.EntityFrameworkCore;
using oop_s3_1_mvc_83303.Data;
using oop_s3_1_mvc_83303.Models;
using oop_s3_1_mvc_83303.Services;
using Xunit;

namespace oop_s3_1_mvc_83303.Tests
{
    public class SecurityAndBusinessTests
    {
        private readonly GradebookService _service;

        public SecurityAndBusinessTests()
        {
            _service = new GradebookService();
        }

        [Fact]
        public void CalculateAverage_EmptyScores_ReturnsZero()
        {
            var scores = new List<double>();
            var result = _service.CalculateAverage(scores);
            Assert.Equal(0, result);
        }

        [Fact]
        public void CalculateAverage_MultipleScores_ReturnsCorrectAverage()
        {
            var scores = new List<double> { 80, 90, 100 };
            var result = _service.CalculateAverage(scores);
            Assert.Equal(90, result);
        }

        [Fact]
        public void GetGrade_ScoreBoundaries_ReturnsCorrectGrade()
        {
            Assert.Equal("A", _service.GetGrade(70, 100));
            Assert.Equal("B", _service.GetGrade(60, 100));
            Assert.Equal("C", _service.GetGrade(50, 100));
            Assert.Equal("D", _service.GetGrade(40, 100));
            Assert.Equal("F", _service.GetGrade(30, 100));
        }

        [Fact]
        public void CanStudentSeeExamResult_Released_ReturnsTrue()
        {
            var exam = new Exam { ResultsReleased = true };
            Assert.True(_service.CanStudentSeeExamResult(exam));
        }

        [Fact]
        public void CanStudentSeeExamResult_NotReleased_ReturnsFalse()
        {
            var exam = new Exam { ResultsReleased = false };
            Assert.False(_service.CanStudentSeeExamResult(exam));
        }

        [Fact]
        public void IsStudentEnrolled_Enrolled_ReturnsTrue()
        {
            var enrolments = new List<CourseEnrolment>
            {
                new CourseEnrolment { StudentProfileId = 1, CourseId = 10 }
            };
            Assert.True(_service.IsStudentEnrolled(1, 10, enrolments));
        }

        [Fact]
        public void IsStudentEnrolled_NotEnrolled_ReturnsFalse()
        {
            var enrolments = new List<CourseEnrolment>
            {
                new CourseEnrolment { StudentProfileId = 1, CourseId = 10 }
            };
            Assert.False(_service.IsStudentEnrolled(1, 11, enrolments));
        }

        [Fact]
        public async Task Database_ExamVisibilityRule_CorrectlyFilters()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "ExamVisibilityTest")
                .Options;

            using (var context = new ApplicationDbContext(options))
            {
                var exam1 = new Exam { Id = 1, Title = "Released", ResultsReleased = true };
                var exam2 = new Exam { Id = 2, Title = "Hidden", ResultsReleased = false };
                context.Exams.AddRange(exam1, exam2);
                await context.SaveChangesAsync();

                var visibleExams = await context.Exams.Where(e => e.ResultsReleased).ToListAsync();
                Assert.Single(visibleExams);
                Assert.Equal("Released", visibleExams[0].Title);
            }
        }

        [Fact]
        public void CanFacultyAccessCourse_CorrectFaculty_ReturnsTrue()
        {
            var course = new Course { FacultyProfileId = 5 };
            Assert.True(_service.CanFacultyAccessCourse(5, course));
        }

        [Fact]
        public void CanFacultyAccessCourse_WrongFaculty_ReturnsFalse()
        {
            var course = new Course { FacultyProfileId = 5 };
            Assert.False(_service.CanFacultyAccessCourse(6, course));
        }

        [Fact]
        public void GetGrade_HandlesDecimalScores()
        {
            // Percentage = 69.5, rounded to 70. Should return A.
            Assert.Equal("A", _service.GetGrade(69.5, 100));
        }
    }
}
