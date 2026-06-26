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

-- Seed Users (Password is '123456' hashed with BCrypt work factor 11)
INSERT INTO "User" ("Username", "PasswordHash", "Role")
VALUES
('admin', '$2a$12$61eNRjOYrMbOF/DZDt37NOrnJ78NrEszd91M9jJEQbC3KHb3SvuRy', 'Admin'),
('student', '$2a$12$61eNRjOYrMbOF/DZDt37NOrnJ78NrEszd91M9jJEQbC3KHb3SvuRy', 'Student');
