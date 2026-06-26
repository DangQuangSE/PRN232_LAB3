using FluentValidation;
using PRN232.LMSSystem.StudentService.Entities;
using PRN232.LMSSystem.StudentService.Interfaces;
using PRN232.LMSSystem.StudentService.Exceptions;
using PRN232.LMSSystem.StudentService.Helpers;
using PRN232.LMSSystem.StudentService.Models.Query;
using PRN232.LMSSystem.StudentService.Models.Request;
using PRN232.LMSSystem.StudentService.Models.Response;
using System.Linq.Expressions;

namespace PRN232.LMSSystem.StudentService.Services;

public class StudentService : IStudentService
{
    private readonly IStudentRepository _studentRepository;
    private readonly IValidator<CreateStudentRequest> _validator;

    public StudentService(IStudentRepository studentRepository, IValidator<CreateStudentRequest> validator)
    {
        _studentRepository = studentRepository;
        _validator = validator;
    }

    public async Task<(IEnumerable<StudentResponse> Data, PaginationMetadata Pagination)> GetAllAsync(QueryParameters queryParams)
    {
        Expression<Func<Student, bool>>? filter = null;
        
        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var searchLower = queryParams.Search.ToLower().Trim();
            filter = s => s.FullName.ToLower().Contains(searchLower) 
                       || s.Email.ToLower().Contains(searchLower)
                       || s.StudentCode.ToLower().Contains(searchLower)
                       || s.Phone.ToLower().Contains(searchLower);
        }

        int totalItems = await _studentRepository.CountAsync(filter);
        var pagination = new PaginationMetadata(queryParams.Page, queryParams.PageSize, totalItems);

        Func<IQueryable<Student>, IOrderedQueryable<Student>>? orderBy = null;
        if (!string.IsNullOrWhiteSpace(queryParams.Sort))
            orderBy = q => (IOrderedQueryable<Student>)QueryHelper.ApplySort(q, queryParams.Sort);
        else
            orderBy = q => q.OrderBy(s => s.StudentId);

        var students = await _studentRepository.GetAllAsync(
            filter: filter,
            orderBy: orderBy,
            includeProperties: null,
            page: queryParams.Page,
            pageSize: queryParams.PageSize
        );

        return (students.Select(s => MapToResponse(s)), pagination);
    }

    public async Task<StudentResponse> GetByIdAsync(int id, string? expand = null)
    {
        var student = await _studentRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Student", id);

        return MapToResponse(student);
    }

    public async Task<StudentResponse> CreateAsync(CreateStudentRequest request)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new BadRequestException("Validation failed", errors);
        }

        var existingStudent = await _studentRepository.GetAllAsync(s => s.StudentCode.ToLower() == request.StudentCode.ToLower().Trim());
        if (existingStudent.Any())
        {
            throw new BadRequestException("StudentCode is already in use.", new Dictionary<string, string[]> {
                { "studentCode", new[] { $"Student code '{request.StudentCode}' is already registered." } }
            });
        }

        var student = new Student
        {
            StudentCode = request.StudentCode.Trim().ToUpper(),
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            DateOfBirth = DateTime.SpecifyKind(request.DateOfBirth, DateTimeKind.Utc)
        };

        await _studentRepository.AddAsync(student);
        await _studentRepository.SaveAsync();

        return MapToResponse(student);
    }

    public async Task UpdateAsync(int id, CreateStudentRequest request)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new BadRequestException("Validation failed", errors);
        }

        var student = await _studentRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Student", id);

        var existingStudent = await _studentRepository.GetAllAsync(s => s.StudentCode.ToLower() == request.StudentCode.ToLower().Trim() && s.StudentId != id);
        if (existingStudent.Any())
        {
            throw new BadRequestException("StudentCode is already in use.", new Dictionary<string, string[]> {
                { "studentCode", new[] { $"Student code '{request.StudentCode}' is already registered." } }
            });
        }

        student.StudentCode = request.StudentCode.Trim().ToUpper();
        student.FullName = request.FullName.Trim();
        student.Email = request.Email.Trim();
        student.Phone = request.Phone.Trim();
        student.DateOfBirth = DateTime.SpecifyKind(request.DateOfBirth, DateTimeKind.Utc);

        _studentRepository.Update(student);
        await _studentRepository.SaveAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var student = await _studentRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Student", id);

        _studentRepository.Delete(student);
        await _studentRepository.SaveAsync();
    }

    private StudentResponse MapToResponse(Student student)
    {
        return new StudentResponse
        {
            StudentId = student.StudentId,
            StudentCode = student.StudentCode,
            FullName = student.FullName,
            Email = student.Email,
            Phone = student.Phone,
            DateOfBirth = student.DateOfBirth,
            Enrollments = null
        };
    }
}
