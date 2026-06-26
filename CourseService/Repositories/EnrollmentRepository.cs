using PRN232.LMSSystem.CourseService.Data;
using PRN232.LMSSystem.CourseService.Entities;
using PRN232.LMSSystem.CourseService.Interfaces;

namespace PRN232.LMSSystem.CourseService.Repositories;

public class EnrollmentRepository : GenericRepository<Enrollment>, IEnrollmentRepository
{
    public EnrollmentRepository(CourseDbContext context) : base(context)
    {
    }
}
