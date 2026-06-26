namespace PRN232.LMSSystem.CourseService.Models.Response;

public class CourseResponse
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public int SemesterId { get; set; }
    public string? SemesterName { get; set; }
    public int EnrollmentCount { get; set; }
    public SemesterBriefResponse? Semester { get; set; }
    public IEnumerable<CourseEnrollmentResponse>? Enrollments { get; set; }
}

public class CourseBriefResponse
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public int SemesterId { get; set; }
    public string? SemesterName { get; set; }
    public int? EnrollmentCount { get; set; }
}

public class CourseEnrollmentResponse
{
    public int EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public DateTime EnrollDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
