using PRN232.LMSSystem.CourseService.Data;
using PRN232.LMSSystem.CourseService.Entities;
using PRN232.LMSSystem.CourseService.Interfaces;

namespace PRN232.LMSSystem.CourseService.Repositories;

public class SemesterRepository : GenericRepository<Semester>, ISemesterRepository
{
    public SemesterRepository(CourseDbContext context) : base(context)
    {
    }
}
