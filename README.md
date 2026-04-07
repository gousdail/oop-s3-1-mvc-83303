# OOP Assessment #3 - MVC College Management System

This project is an ASP.NET Core MVC application for managing a college system, including students, faculty, branches, courses, exams, and attendance.

## Project Information
- **Framework:** .NET 8.0
- **Database:** SQL Server (Entity Framework Core)
- **Branch:** `master`

## Features
- **Identity Management:** Role-based access control (Admin, Faculty, Student).
- **Gradebook:** Complex grade calculations and average tracking.
- **Attendance:** Faculty can record and track student attendance.
- **Exams & Assignments:** Management of assessments and results.
- **Security:** Strict data visibility rules based on user roles.

## Test Credentials
The database is automatically seeded with the following accounts for testing:

| Role | Email | Password |
|------|-------|----------|
| **Admin** | `admin@college.com` | `Admin123!` |
| **Faculty (Dublin)** | `faculty1@college.com` | `Faculty123!` |
| **Faculty (Cork)** | `faculty2@college.com` | `Faculty123!` |
| **Faculty (Galway)** | `faculty3@college.com` | `Faculty123!` |
| **Student 1** | `student1@college.com` | `Student123!` |
| **Student 5** | `student5@college.com` | `Student123!` |

## Development & Testing

### Irish Branch Locations
- **Dublin Campus**: O'Connell St, Dublin 1
- **Cork Campus**: Grand Parade, Cork
- **Galway Campus**: Eyre Square, Galway

### Running the Application
```bash
dotnet run --project src/oop-s3-1-mvc-83303/oop-s3-1-mvc-83303.csproj
```

### Running Tests
To run all unit tests:
```bash
dotnet test
```

### Code Coverage Report
The project is configured with GitHub Actions to generate a coverage report using `reportgenerator`.
To generate the report locally:
1. Install the tool: `dotnet tool install --global dotnet-reportgenerator-globaltool`
2. Run tests with coverage: `dotnet test --collect:"XPlat Code Coverage"`
3. Generate HTML: `reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html`
4. Open `coveragereport/index.html` in your browser.

## CI/CD
The project uses GitHub Actions (`.github/workflows/ci.yml`) to:
1. Build and test the application on every push to `master`.
2. Generate a code coverage report.
3. Automatically deploy the coverage report to **GitHub Pages**.

## Key Design Decisions
- **Model Property:** `Exam.Name` has been renamed to `Exam.Title` for clarity and consistency.
- **Coverage:** Migration files and `Program.cs` are excluded from coverage to focus on business logic metrics.
