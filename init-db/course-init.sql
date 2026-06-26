CREATE TABLE "Semester" (
    "SemesterId" SERIAL PRIMARY KEY,
    "SemesterName" VARCHAR(100) NOT NULL,
    "StartDate" TIMESTAMP NOT NULL,
    "EndDate" TIMESTAMP NOT NULL
);

CREATE TABLE "Course" (
    "CourseId" SERIAL PRIMARY KEY,
    "CourseName" VARCHAR(100) NOT NULL,
    "SemesterId" INT NOT NULL REFERENCES "Semester"("SemesterId") ON DELETE CASCADE
);

CREATE TABLE "Subject" (
    "SubjectId" SERIAL PRIMARY KEY,
    "SubjectCode" VARCHAR(20) NOT NULL,
    "SubjectName" VARCHAR(100) NOT NULL,
    "Credit" INT NOT NULL
);

CREATE TABLE "Enrollment" (
    "EnrollmentId" SERIAL PRIMARY KEY,
    "StudentId" INT NOT NULL,
    "CourseId" INT NOT NULL REFERENCES "Course"("CourseId") ON DELETE CASCADE,
    "EnrollDate" TIMESTAMP NOT NULL,
    "Status" VARCHAR(20) NOT NULL,
    UNIQUE ("StudentId", "CourseId")
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

-- Seed Enrollments
INSERT INTO "Enrollment" ("StudentId", "CourseId", "EnrollDate", "Status")
SELECT
    floor(random() * 50 + 1)::int,
    floor(random() * 20 + 1)::int,
    NOW() - (random() * 100 * interval '1 day'),
    CASE WHEN random() > 0.5 THEN 'Active' ELSE 'Completed' END
FROM generate_series(1, 500) AS i
ON CONFLICT DO NOTHING;
