using PRN232.LMSSystem.CourseService.Entities;
using PRN232.LMSSystem.CourseService.Interfaces;
using PRN232.LMSSystem.CourseService.Exceptions;
using PRN232.LMSSystem.CourseService.Helpers;
using PRN232.LMSSystem.CourseService.Models.Query;
using PRN232.LMSSystem.CourseService.Models.Request;
using PRN232.LMSSystem.CourseService.Models.Response;
using System.Linq.Expressions;

namespace PRN232.LMSSystem.CourseService.Services;

public class SubjectService : ISubjectService
{
    private readonly ISubjectRepository _subjectRepository;

    public SubjectService(ISubjectRepository subjectRepository)
    {
        _subjectRepository = subjectRepository;
    }

    public async Task<(IEnumerable<SubjectResponse> Data, PaginationMetadata Pagination)> GetAllAsync(QueryParameters queryParams)
    {
        Expression<Func<Subject, bool>>? filter = null;
        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var searchLower = queryParams.Search.ToLower().Trim();
            filter = s => s.SubjectCode.ToLower().Contains(searchLower) || s.SubjectName.ToLower().Contains(searchLower);
        }

        int totalItems = await _subjectRepository.CountAsync(filter);
        var pagination = new PaginationMetadata(queryParams.Page, queryParams.PageSize, totalItems);

        Func<IQueryable<Subject>, IOrderedQueryable<Subject>>? orderBy = null;
        if (!string.IsNullOrWhiteSpace(queryParams.Sort))
            orderBy = q => (IOrderedQueryable<Subject>)QueryHelper.ApplySort(q, queryParams.Sort);
        else
            orderBy = q => q.OrderBy(s => s.SubjectId);

        var subjects = await _subjectRepository.GetAllAsync(
            filter: filter,
            orderBy: orderBy,
            page: queryParams.Page,
            pageSize: queryParams.PageSize
        );

        return (subjects.Select(MapToResponse), pagination);
    }

    public async Task<SubjectResponse> GetByIdAsync(int id, string? expand = null)
    {
        var subject = await _subjectRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Subject", id);

        return MapToResponse(subject);
    }

    public async Task<SubjectResponse> CreateAsync(SubjectRequest request)
    {
        var subject = new Subject
        {
            SubjectCode = request.SubjectCode,
            SubjectName = request.SubjectName,
            Credit = request.Credit
        };

        await _subjectRepository.AddAsync(subject);
        await _subjectRepository.SaveAsync();

        return MapToResponse(subject);
    }

    public async Task UpdateAsync(int id, SubjectRequest request)
    {
        var subject = await _subjectRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Subject", id);

        subject.SubjectCode = request.SubjectCode;
        subject.SubjectName = request.SubjectName;
        subject.Credit = request.Credit;

        _subjectRepository.Update(subject);
        await _subjectRepository.SaveAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var subject = await _subjectRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Subject", id);

        _subjectRepository.Delete(subject);
        await _subjectRepository.SaveAsync();
    }

    private SubjectResponse MapToResponse(Subject subject)
    {
        return new SubjectResponse
        {
            SubjectId = subject.SubjectId,
            SubjectCode = subject.SubjectCode,
            SubjectName = subject.SubjectName,
            Credit = subject.Credit
        };
    }
}
