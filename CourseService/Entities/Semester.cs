namespace PRN232.LMSSystem.CourseService.Entities;

public class Semester
{
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
