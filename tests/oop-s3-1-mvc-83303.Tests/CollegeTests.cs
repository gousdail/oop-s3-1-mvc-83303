using Microsoft.EntityFrameworkCore;
using oop_s3_1_mvc_83303.Data;
using oop_s3_1_mvc_83303.Models;
using oop_s3_1_mvc_83303.Services;
using Xunit;

namespace oop_s3_1_mvc_83303.Tests
{
    public class CollegeTests
    {
        private readonly GradebookService _service = new GradebookService();

        [Fact]
        public void CalculateAverage_WithScores_ReturnsCorrectAverage()
        {
            var scores = new List<double> { 80, 90, 70 };
            var result = _service.CalculateAverage(scores);
            Assert.Equal(80, result);
        }

        [Fact]
        public void CalculateAverage_EmptyList_ReturnsZero()
        {
            var result = _service.CalculateAverage(new List<double>());
            Assert.Equal(0, result);
        }

        [Fact]
        public void CanStudentSeeExamResult_WhenReleased_ReturnsTrue()
        {
            var exam = new Exam { ResultsReleased = true };
            Assert.True(_service.CanStudentSeeExamResult(exam));
        }

        [Fact]
        public void CanStudentSeeExamResult_WhenNotReleased_ReturnsFalse()
        {
            var exam = new Exam { ResultsReleased = false };
            Assert.False(_service.CanStudentSeeExamResult(exam));
        }

        [Fact]
        public void GetGrade_Above70_ReturnsA()
        {
            Assert.Equal("A", _service.GetGrade(75, 100));
        }

        [Fact]
        public void GetGrade_Below40_ReturnsF()
        {
            Assert.Equal("F", _service.GetGrade(30, 100));
        }

        [Fact]
        public void CanFacultyAccessCourse_Owner_ReturnsTrue()
        {
            var course = new Course { FacultyProfileId = 1 };
            Assert.True(_service.CanFacultyAccessCourse(1, course));
        }

        [Fact]
        public void CanFacultyAccessCourse_NotOwner_ReturnsFalse()
        {
            var course = new Course { FacultyProfileId = 2 };
            Assert.False(_service.CanFacultyAccessCourse(1, course));
        }

        [Fact]
        public void GetGrade_Exactly50_ReturnsC()
        {
            Assert.Equal("C", _service.GetGrade(50, 100));
        }

        [Fact]
        public void GetGrade_Exactly60_ReturnsB()
        {
            Assert.Equal("B", _service.GetGrade(60, 100));
        }
    }
}
