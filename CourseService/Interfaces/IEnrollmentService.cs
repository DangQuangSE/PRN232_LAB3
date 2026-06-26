using PRN232.LMSSystem.CourseService.Models.Query;
using PRN232.LMSSystem.CourseService.Models.Request;
using PRN232.LMSSystem.CourseService.Models.Response;

namespace PRN232.LMSSystem.CourseService.Interfaces;

public interface IEnrollmentService
{
    Task<(IEnumerable<EnrollmentResponse> Data, PaginationMetadata Pagination)> GetAllAsync(QueryParameters queryParams);
    Task<(IEnumerable<EnrollmentOfCourseResponse> Data, PaginationMetadata Pagination)> GetByCourseIdAsync(int courseId, QueryParameters queryParams);
    Task<(IEnumerable<EnrollmentOfStudentResponse> Data, PaginationMetadata Pagination)> GetByStudentIdAsync(int studentId, QueryParameters queryParams);
    Task<EnrollmentResponse> GetByIdAsync(int id, string? expand = null);
    Task<EnrollmentResponse> CreateAsync(EnrollmentRequest request);
    Task UpdateAsync(int id, EnrollmentRequest request);
    Task DeleteAsync(int id);
}
