CREATE TABLE "Semester" (
    "SemesterId" SERIAL PRIMARY KEY,
    "SemesterName" VARCHAR(100) NOT NULL,
    "StartDate" TIMESTAMP NOT NULL,
    "EndDate" TIMESTAMP NOT NULL
);

CREATE TABLE "Course" (
    "CourseId" SERIAL PRIMARY KEY,
    "CourseName" VARCHAR(100) NOT NULL,
    "SemesterId" INT NOT NULL REFERENCES "Semester"("SemesterId")
);

CREATE TABLE "Subject" (
    "SubjectId" SERIAL PRIMARY KEY,
    "SubjectCode" VARCHAR(20) NOT NULL,
    "SubjectName" VARCHAR(100) NOT NULL,
    "Credit" INT NOT NULL
);

CREATE TABLE "Student" (
    "StudentId" SERIAL PRIMARY KEY,
    "StudentCode" VARCHAR(20) NOT NULL UNIQUE,
    "FullName" VARCHAR(100) NOT NULL,
    "Email" VARCHAR(100) NOT NULL,
    "Phone" VARCHAR(20) NOT NULL,
    "DateOfBirth" TIMESTAMP NOT NULL
);

CREATE TABLE "Enrollment" (
    "EnrollmentId" SERIAL PRIMARY KEY,
    "StudentId" INT NOT NULL REFERENCES "Student"("StudentId") ON DELETE CASCADE,
    "CourseId" INT NOT NULL REFERENCES "Course"("CourseId") ON DELETE CASCADE,
    "EnrollDate" TIMESTAMP NOT NULL,
    "Status" VARCHAR(20) NOT NULL
);

CREATE TABLE "User" (
    "UserId" SERIAL PRIMARY KEY,
    "Username" VARCHAR(50) NOT NULL UNIQUE,
    "PasswordHash" VARCHAR(255) NOT NULL,
    "Role" VARCHAR(20) NOT NULL
);

CREATE TABLE "RefreshToken" (
    "RefreshTokenId" SERIAL PRIMARY KEY,
    "Token" VARCHAR(255) NOT NULL UNIQUE,
    "Expires" TIMESTAMP NOT NULL,
    "Created" TIMESTAMP NOT NULL,
    "Revoked" TIMESTAMP,
    "UserId" INT NOT NULL REFERENCES "User"("UserId") ON DELETE CASCADE
);

-- Seed Semesters
INSERT INTO "Semester" ("SemesterName", "StartDate", "EndDate")
SELECT 'Semester ' || i, NOW() + (i * interval '6 months'), NOW() + ((i+1) * interval '6 months')
FROM generate_series(1, 5) AS i;

-- Seed Subjects
INSERT INTO "Subject" ("SubjectCode", "SubjectName", "Credit")
SELECT 'SUB' || i, 'Subject Name ' || i, floor(random() * 3 + 2)::int
FROM generate_series(1, 10) AS i;

-- Seed Courses
INSERT INTO "Course" ("CourseName", "SemesterId")
SELECT 'Course ' || i, floor(random() * 5 + 1)::int
FROM generate_series(1, 20) AS i;

-- Seed Students (with FPTU formatted student codes and valid phone numbers)
INSERT INTO "Student" ("StudentCode", "FullName", "Email", "Phone", "DateOfBirth")
SELECT 
    'SE' || (190000 + i), 
    'Student ' || i, 
    'student' || i || '@fpt.edu.vn', 
    '0908' || LPAD(i::text, 6, '0'), 
    NOW() - (interval '18 years') - (random() * 1000 * interval '1 day')
FROM generate_series(1, 50) AS i;

-- Seed Enrollments
INSERT INTO "Enrollment" ("StudentId", "CourseId", "EnrollDate", "Status")
SELECT
    floor(random() * 50 + 1)::int,
    floor(random() * 20 + 1)::int,
    NOW() - (random() * 100 * interval '1 day'),
    CASE WHEN random() > 0.5 THEN 'Active' ELSE 'Completed' END
FROM generate_series(1, 500) AS i
ON CONFLICT DO NOTHING;

-- Seed Users (Password is '123456' hashed with BCrypt work factor 11)
INSERT INTO "User" ("Username", "PasswordHash", "Role")
VALUES
('admin', '$2a$12$61eNRjOYrMbOF/DZDt37NOrnJ78NrEszd91M9jJEQbC3KHb3SvuRy', 'Admin'),
('student', '$2a$12$61eNRjOYrMbOF/DZDt37NOrnJ78NrEszd91M9jJEQbC3KHb3SvuRy', 'Student');
