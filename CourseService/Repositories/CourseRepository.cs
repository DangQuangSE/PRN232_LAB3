using Microsoft.EntityFrameworkCore;
using PRN232.LMSSystem.CourseService.Data;
using PRN232.LMSSystem.CourseService.Entities;
using PRN232.LMSSystem.CourseService.Interfaces;
using System.Linq.Expressions;

namespace PRN232.LMSSystem.CourseService.Repositories;

public class CourseRepository : GenericRepository<Course>, ICourseRepository
{
    public CourseRepository(CourseDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<CourseWithCount>> GetCoursesWithCountAsync(
        Expression<Func<Course, bool>>? filter = null,
        Func<IQueryable<Course>, IOrderedQueryable<Course>>? orderBy = null,
        int? page = null,
        int? pageSize = null,
        bool includeEnrollments = false)
    {
        IQueryable<Course> query = _dbSet;

        if (filter != null)
            query = query.Where(filter);

        query = query.Include(c => c.Semester);

        if (includeEnrollments)
            query = query.Include(c => c.Enrollments);

        if (orderBy != null)
            query = orderBy(query);

        if (page.HasValue && pageSize.HasValue && page.Value > 0 && pageSize.Value > 0)
        {
            int skip = (page.Value - 1) * pageSize.Value;
            query = query.Skip(skip).Take(pageSize.Value);
        }

        if (includeEnrollments)
        {
            var courses = await query.ToListAsync();
            return courses.Select(c => new CourseWithCount
            {
                Course = c,
                EnrollmentCount = c.Enrollments.Count
            });
        }

        return await query.Select(c => new CourseWithCount
        {
            Course = c,
            EnrollmentCount = c.Enrollments.Count
        }).ToListAsync();
    }

    public async Task<CourseWithCount?> GetCourseWithCountByIdAsync(int id, bool includeEnrollments = false)
    {
        var query = _dbSet.Where(c => c.CourseId == id).Include(c => c.Semester);

        if (includeEnrollments)
        {
            var course = await query.Include(c => c.Enrollments)
                .FirstOrDefaultAsync();
            if (course == null) return null;
            return new CourseWithCount { Course = course, EnrollmentCount = course.Enrollments.Count };
        }

        return await query.Select(c => new CourseWithCount
        {
            Course = c,
            EnrollmentCount = c.Enrollments.Count
        }).FirstOrDefaultAsync();
    }
}
