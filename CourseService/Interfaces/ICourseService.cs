using PRN232.LMSSystem.CourseService.Models.Query;
using PRN232.LMSSystem.CourseService.Models.Request;
using PRN232.LMSSystem.CourseService.Models.Response;

namespace PRN232.LMSSystem.CourseService.Interfaces;

public interface ICourseService
{
    Task<(IEnumerable<CourseResponse> Data, PaginationMetadata Pagination)> GetAllAsync(QueryParameters queryParams);
    Task<(IEnumerable<CourseBriefResponse> Data, PaginationMetadata Pagination)> GetBySemesterIdAsync(int semesterId, QueryParameters queryParams);
    Task<CourseResponse> GetByIdAsync(int id, string? expand = null);
    Task<CourseResponse> CreateAsync(CourseRequest request);
    Task UpdateAsync(int id, CourseRequest request);
    Task DeleteAsync(int id);
}
