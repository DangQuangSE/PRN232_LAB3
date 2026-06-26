using PRN232.LMSSystem.StudentService.Models.Query;
using PRN232.LMSSystem.StudentService.Models.Request;
using PRN232.LMSSystem.StudentService.Models.Response;

namespace PRN232.LMSSystem.StudentService.Interfaces;

public interface IStudentService
{
    Task<(IEnumerable<StudentResponse> Data, PaginationMetadata Pagination)> GetAllAsync(QueryParameters queryParams);
    Task<StudentResponse> GetByIdAsync(int id, string? expand = null);
    Task<StudentResponse> CreateAsync(CreateStudentRequest request);
    Task UpdateAsync(int id, CreateStudentRequest request);
    Task DeleteAsync(int id);
}
