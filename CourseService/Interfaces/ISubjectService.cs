using PRN232.LMSSystem.CourseService.Models.Query;
using PRN232.LMSSystem.CourseService.Models.Request;
using PRN232.LMSSystem.CourseService.Models.Response;

namespace PRN232.LMSSystem.CourseService.Interfaces;

public interface ISubjectService
{
    Task<(IEnumerable<SubjectResponse> Data, PaginationMetadata Pagination)> GetAllAsync(QueryParameters queryParams);
    Task<SubjectResponse> GetByIdAsync(int id, string? expand = null);
    Task<SubjectResponse> CreateAsync(SubjectRequest request);
    Task UpdateAsync(int id, SubjectRequest request);
    Task DeleteAsync(int id);
}
