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
        public async Task Database_CanAddBranch()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_Branch")
                .Options;

            using (var context = new ApplicationDbContext(options))
            {
                context.Branches.Add(new Branch { Name = "Test Branch", Address = "Test Addr" });
                await context.SaveChangesAsync();
                Assert.Equal(1, await context.Branches.CountAsync());
            }
        }

        [Fact]
        public async Task Database_CanAddStudentProfile()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_Student")
                .Options;

            using (var context = new ApplicationDbContext(options))
            {
                context.StudentProfiles.Add(new StudentProfile { Name = "Test Student", StudentNumber = "S123" });
                await context.SaveChangesAsync();
                Assert.Equal(1, await context.StudentProfiles.CountAsync());
            }
        }
    }
}
