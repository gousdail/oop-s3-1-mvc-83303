# Acme Global College - Management System

## Project Overview
This is a .NET 8 ASP.NET Core MVC application for managing courses, students, faculty, and academic results across multiple branches.

## Features
- **RBAC Security**: 3 roles (Admin, Faculty, Student).
- **Academic Tracking**: Attendance, Gradebook (Assignments/Exams).
- **Visibility Rules**: Students cannot see provisional exam results.
- **Seeded Data**: Automatic database creation and seeding.

## Setup Instructions
To get the project running locally:

1.  **Restore Packages**:
    ```bash
    dotnet restore
    ```
2.  **Update Database**:
    Ensures the local SQL Server database is created and up to date with migrations.
    ```bash
    dotnet ef database update
    ```
3.  **Run Application**:
    ```bash
    dotnet run
    ```

## Test Credentials
The database is automatically seeded with the following accounts:

| Role | Email | Password |
|------|-------|----------|
| **Admin** | `admin@college.com` | `Admin123!` |
| **Faculty** | `faculty@college.com` | `Faculty123!` |
| **Student** | `student1@college.com` | `Student123!` |
| **Student** | `student2@college.com` | `Student123!` |

## Running Tests
To verify the business logic and security rules:
```bash
dotnet test
```
The test suite includes 10 robust tests covering GPA calculations, grade visibility rules, and access restrictions.
