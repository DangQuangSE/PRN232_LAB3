CREATE TABLE "Student" (
    "StudentId" SERIAL PRIMARY KEY,
    "StudentCode" VARCHAR(20) NOT NULL UNIQUE,
    "FullName" VARCHAR(100) NOT NULL,
    "Email" VARCHAR(100) NOT NULL,
    "Phone" VARCHAR(20) NOT NULL,
    "DateOfBirth" TIMESTAMP NOT NULL
);

-- Seed Students
INSERT INTO "Student" ("StudentCode", "FullName", "Email", "Phone", "DateOfBirth")
SELECT 
    'SE' || (190000 + i), 
    'Student ' || i, 
    'student' || i || '@fpt.edu.vn', 
    '0908' || LPAD(i::text, 6, '0'), 
    NOW() - (interval '18 years') - (random() * 1000 * interval '1 day')
FROM generate_series(1, 50) AS i;
