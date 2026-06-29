namespace PRN232.LMSSystem.Messages;

public record EnrollmentCreatedEvent
{
    public int EnrollmentId { get; init; }
    public int StudentId { get; init; }
    public int CourseId { get; init; }
    public string? CourseName { get; init; }
    public DateTime EnrollDate { get; init; }
    public string Status { get; init; } = "";
}
