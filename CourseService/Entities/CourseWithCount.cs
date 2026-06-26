namespace PRN232.LMSSystem.CourseService.Entities;

public class CourseWithCount
{
    public Course Course { get; set; } = null!;
    public int EnrollmentCount { get; set; }
}
