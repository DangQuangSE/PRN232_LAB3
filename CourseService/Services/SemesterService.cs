using FluentValidation;
using PRN232.LMSSystem.CourseService.Entities;
using PRN232.LMSSystem.CourseService.Interfaces;
using PRN232.LMSSystem.CourseService.Exceptions;
using PRN232.LMSSystem.CourseService.Helpers;
using PRN232.LMSSystem.CourseService.Models.Query;
using PRN232.LMSSystem.CourseService.Models.Request;
using PRN232.LMSSystem.CourseService.Models.Response;
using System.Linq.Expressions;

namespace PRN232.LMSSystem.CourseService.Services;

public class SemesterService : ISemesterService
{
    private readonly ISemesterRepository _semesterRepository;
    private readonly IValidator<SemesterRequest> _validator;

    public SemesterService(ISemesterRepository semesterRepository, IValidator<SemesterRequest> validator)
    {
        _semesterRepository = semesterRepository;
        _validator = validator;
    }

    public async Task<(IEnumerable<SemesterResponse> Data, PaginationMetadata Pagination)> GetAllAsync(QueryParameters queryParams)
    {
        Expression<Func<Semester, bool>>? filter = null;
        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var searchLower = queryParams.Search.ToLower().Trim();
            filter = s => s.SemesterName.ToLower().Contains(searchLower);
        }

        int totalItems = await _semesterRepository.CountAsync(filter);
        var pagination = new PaginationMetadata(queryParams.Page, queryParams.PageSize, totalItems);

        Func<IQueryable<Semester>, IOrderedQueryable<Semester>>? orderBy = null;
        if (!string.IsNullOrWhiteSpace(queryParams.Sort))
            orderBy = q => (IOrderedQueryable<Semester>)QueryHelper.ApplySort(q, queryParams.Sort);
        else
            orderBy = q => q.OrderBy(s => s.SemesterId);

        var semesters = await _semesterRepository.GetAllAsync(
            filter: filter,
            orderBy: orderBy,
            includeProperties: new List<string> { "Courses" },
            page: queryParams.Page,
            pageSize: queryParams.PageSize
        );

        return (semesters.Select(s => MapToResponse(s, queryParams.Expand)), pagination);
    }

    public async Task<SemesterResponse> GetByIdAsync(int id, string? expand = null)
    {
        var semester = await _semesterRepository.GetByIdAsync(id, new List<string> { "Courses" })
            ?? throw new NotFoundException("Semester", id);

        return MapToResponse(semester, expand);
    }

    public async Task<SemesterResponse> CreateAsync(SemesterRequest request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new BadRequestException("Validation failed.", validation.Errors.Select(e => e.ErrorMessage));

        var semester = new Semester
        {
            SemesterName = request.SemesterName,
            StartDate = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(request.EndDate, DateTimeKind.Utc)
        };

        await _semesterRepository.AddAsync(semester);
        await _semesterRepository.SaveAsync();

        return MapToResponse(semester);
    }

    public async Task UpdateAsync(int id, SemesterRequest request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new BadRequestException("Validation failed.", validation.Errors.Select(e => e.ErrorMessage));

        var semester = await _semesterRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Semester", id);

        semester.SemesterName = request.SemesterName;
        semester.StartDate = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc);
        semester.EndDate = DateTime.SpecifyKind(request.EndDate, DateTimeKind.Utc);

        _semesterRepository.Update(semester);
        await _semesterRepository.SaveAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var semester = await _semesterRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Semester", id);

        _semesterRepository.Delete(semester);
        await _semesterRepository.SaveAsync();
    }

    private SemesterResponse MapToResponse(Semester semester, string? expand = null)
    {
        var response = new SemesterResponse
        {
            SemesterId = semester.SemesterId,
            SemesterName = semester.SemesterName,
            StartDate = semester.StartDate,
            EndDate = semester.EndDate,
            CourseCount = semester.Courses?.Count ?? 0
        };

        if (!string.IsNullOrWhiteSpace(expand))
        {
            var expands = expand.ToLower().Split(',');
            if (expands.Contains("courses") && semester.Courses != null)
            {
                response.Courses = semester.Courses.Select(c => new SemesterCourseResponse
                {
                    CourseId = c.CourseId,
                    CourseName = c.CourseName
                }).ToList();
            }
        }

        return response;
    }
}
