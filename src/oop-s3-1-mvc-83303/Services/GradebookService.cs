using oop_s3_1_mvc_83303.Models;

namespace oop_s3_1_mvc_83303.Services
{
    public class GradebookService
    {
        public double CalculateAverage(IEnumerable<double> scores)
        {
            if (!scores.Any()) return 0;
            return scores.Average();
        }

        public bool CanStudentSeeExamResult(Exam exam)
        {
            return exam.ResultsReleased;
        }

        public string GetGrade(double score, double maxScore)
        {
            var percentage = (score / maxScore) * 100;
            if (percentage >= 70) return "A";
            if (percentage >= 60) return "B";
            if (percentage >= 50) return "C";
            if (percentage >= 40) return "D";
            return "F";
        }

        public bool CanFacultyAccessCourse(int facultyId, Course course)
        {
            return course.FacultyProfileId == facultyId;
        }
    }
}
