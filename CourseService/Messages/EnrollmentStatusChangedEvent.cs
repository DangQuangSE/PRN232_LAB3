namespace PRN232.LMSSystem.Messages;

public record EnrollmentStatusChangedEvent
{
    public int EnrollmentId { get; init; }
    public int StudentId { get; init; }
    public int CourseId { get; init; }
    public string OldStatus { get; init; } = "";
    public string NewStatus { get; init; } = "";
}
