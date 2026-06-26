namespace PRN232.LMSSystem.CourseService.Models.Response;

public class EnrollmentResponse
{
    public int EnrollmentId { get; set; }

    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public StudentBriefResponse? Student { get; set; }

    public int CourseId { get; set; }
    public string? CourseName { get; set; }
    public CourseBriefResponse? Course { get; set; }

    public DateTime EnrollDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class EnrollmentOfCourseResponse
{
    public int EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public StudentBriefResponse? Student { get; set; }
    public DateTime EnrollDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class EnrollmentOfStudentResponse
{
    public int EnrollmentId { get; set; }
    public int CourseId { get; set; }
    public string? CourseName { get; set; }
    public CourseBriefResponse? Course { get; set; }
    public DateTime EnrollDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
