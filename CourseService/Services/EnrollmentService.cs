using PRN232.LMSSystem.CourseService.Entities;
using PRN232.LMSSystem.CourseService.Interfaces;
using PRN232.LMSSystem.CourseService.Exceptions;
using PRN232.LMSSystem.CourseService.Helpers;
using PRN232.LMSSystem.CourseService.Models.Query;
using PRN232.LMSSystem.CourseService.Models.Request;
using PRN232.LMSSystem.CourseService.Models.Response;
using PRN232.LMSSystem.Grpc;
using PRN232.LMSSystem.Messages;
using GrpcStudentResponse = PRN232.LMSSystem.Grpc.StudentResponse;
using System.Linq.Expressions;
using Grpc.Core;
using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace PRN232.LMSSystem.CourseService.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly StudentGrpc.StudentGrpcClient _grpcClient;
    private readonly IDistributedCache _cache;
    private readonly IPublishEndpoint _publishEndpoint;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public EnrollmentService(
        IEnrollmentRepository enrollmentRepository,
        StudentGrpc.StudentGrpcClient grpcClient,
        IDistributedCache cache,
        IPublishEndpoint publishEndpoint)
    {
        _enrollmentRepository = enrollmentRepository;
        _grpcClient = grpcClient;
        _cache = cache;
        _publishEndpoint = publishEndpoint;
    }

    private static string StudentCacheKey(int studentId) => $"student:{studentId}";

    private async Task<GrpcStudentResponse?> GetStudentFromCacheAsync(int studentId)
    {
        var json = await _cache.GetStringAsync(StudentCacheKey(studentId));
        if (json is null) return null;
        return JsonSerializer.Deserialize<GrpcStudentResponse>(json);
    }

    private async Task SetStudentCacheAsync(GrpcStudentResponse student)
    {
        var json = JsonSerializer.Serialize(student);
        await _cache.SetStringAsync(
            StudentCacheKey(student.StudentId),
            json,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl });
    }

    private async Task<Dictionary<int, GrpcStudentResponse>> FetchStudentsMapAsync(IEnumerable<int> studentIds)
    {
        var studentMap = new Dictionary<int, GrpcStudentResponse>();
        var distinctIds = studentIds.Distinct().ToList();
        if (!distinctIds.Any()) return studentMap;

        // Check cache first
        var missIds = new List<int>();
        foreach (var id in distinctIds)
        {
            var cached = await GetStudentFromCacheAsync(id);
            if (cached != null)
                studentMap[id] = cached;
            else
                missIds.Add(id);
        }

        if (!missIds.Any()) return studentMap;

        // Batch gRPC call for cache misses only
        try
        {
            var grpcReq = new StudentsRequest();
            grpcReq.StudentIds.AddRange(missIds);
            var grpcRes = await _grpcClient.GetStudentsByIdsAsync(grpcReq);
            foreach (var s in grpcRes.Students)
            {
                studentMap[s.StudentId] = s;
                await SetStudentCacheAsync(s);
            }
        }
        catch (Exception)
        {
            // fallback: return whatever we got from cache
        }
        return studentMap;
    }

    private async Task VerifyStudentExistsAsync(int studentId)
    {
        try
        {
            var verifyRes = await _grpcClient.VerifyStudentAsync(new StudentRequest { StudentId = studentId });
            if (!verifyRes.Exists)
            {
                throw new BadRequestException("Student verification failed.", new Dictionary<string, string[]> {
                    { "studentId", new[] { $"Student with ID {studentId} does not exist." } }
                });
            }
        }
        catch (RpcException ex) when (ex.StatusCode != StatusCode.InvalidArgument)
        {
            throw new BadRequestException("Failed to verify student via gRPC: " + ex.Status.Detail);
        }
    }

    public async Task<(IEnumerable<EnrollmentResponse> Data, PaginationMetadata Pagination)> GetAllAsync(QueryParameters queryParams)
    {
        var includes = new List<string> { "Course" };

        Expression<Func<Enrollment, bool>>? filter = null;
        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var searchLower = queryParams.Search.ToLower().Trim();
            filter = e => e.Status.ToLower().Contains(searchLower) ||
                          e.Course.CourseName.ToLower().Contains(searchLower);
        }

        int totalItems = await _enrollmentRepository.CountAsync(filter);
        var pagination = new PaginationMetadata(queryParams.Page, queryParams.PageSize, totalItems);

        if (!string.IsNullOrWhiteSpace(queryParams.Expand))
        {
            var expands = queryParams.Expand.ToLower().Split(',');
            if (expands.Contains("course"))
                includes.Add("Course.Semester");
        }

        Func<IQueryable<Enrollment>, IOrderedQueryable<Enrollment>>? orderBy = null;
        if (!string.IsNullOrWhiteSpace(queryParams.Sort))
            orderBy = q => (IOrderedQueryable<Enrollment>)QueryHelper.ApplySort(q, queryParams.Sort);
        else
            orderBy = q => q.OrderBy(e => e.EnrollmentId);

        var enrollments = await _enrollmentRepository.GetAllAsync(
            filter: filter,
            orderBy: orderBy,
            includeProperties: includes,
            page: queryParams.Page,
            pageSize: queryParams.PageSize
        );

        var studentIds = enrollments.Select(e => e.StudentId).ToList();
        var studentsMap = await FetchStudentsMapAsync(studentIds);

        return (enrollments.Select(e => MapToResponse(e, studentsMap, queryParams.Expand)), pagination);
    }

    public async Task<(IEnumerable<EnrollmentOfCourseResponse> Data, PaginationMetadata Pagination)> GetByCourseIdAsync(int courseId, QueryParameters queryParams)
    {
        Expression<Func<Enrollment, bool>> baseFilter = e => e.CourseId == courseId;

        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var searchLower = queryParams.Search.ToLower().Trim();
            baseFilter = e => e.CourseId == courseId && e.Status.ToLower().Contains(searchLower);
        }

        int totalItems = await _enrollmentRepository.CountAsync(baseFilter);
        var pagination = new PaginationMetadata(queryParams.Page, queryParams.PageSize, totalItems);

        Func<IQueryable<Enrollment>, IOrderedQueryable<Enrollment>>? orderBy = null;
        if (!string.IsNullOrWhiteSpace(queryParams.Sort))
            orderBy = q => (IOrderedQueryable<Enrollment>)QueryHelper.ApplySort(q, queryParams.Sort);
        else
            orderBy = q => q.OrderBy(e => e.EnrollmentId);

        var enrollments = await _enrollmentRepository.GetAllAsync(
            filter: baseFilter,
            orderBy: orderBy,
            includeProperties: null,
            page: queryParams.Page,
            pageSize: queryParams.PageSize
        );

        var studentIds = enrollments.Select(e => e.StudentId).ToList();
        var studentsMap = await FetchStudentsMapAsync(studentIds);

        var expands = queryParams.Expand?.ToLower().Split(',') ?? [];
        return (enrollments.Select(e => MapToEnrollmentOfCourseResponse(e, studentsMap, expands)), pagination);
    }

    public async Task<(IEnumerable<EnrollmentOfStudentResponse> Data, PaginationMetadata Pagination)> GetByStudentIdAsync(int studentId, QueryParameters queryParams)
    {
        var includes = new List<string> { "Course" };

        if (!string.IsNullOrWhiteSpace(queryParams.Expand))
        {
            var expands = queryParams.Expand.ToLower().Split(',');
            if (expands.Contains("course"))
                includes.Add("Course.Semester");
        }

        Expression<Func<Enrollment, bool>> baseFilter = e => e.StudentId == studentId;

        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var searchLower = queryParams.Search.ToLower().Trim();
            baseFilter = e => e.StudentId == studentId &&
                (e.Status.ToLower().Contains(searchLower) ||
                 e.Course.CourseName.ToLower().Contains(searchLower));
        }

        int totalItems = await _enrollmentRepository.CountAsync(baseFilter);
        var pagination = new PaginationMetadata(queryParams.Page, queryParams.PageSize, totalItems);

        Func<IQueryable<Enrollment>, IOrderedQueryable<Enrollment>>? orderBy = null;
        if (!string.IsNullOrWhiteSpace(queryParams.Sort))
            orderBy = q => (IOrderedQueryable<Enrollment>)QueryHelper.ApplySort(q, queryParams.Sort);
        else
            orderBy = q => q.OrderBy(e => e.EnrollmentId);

        var enrollments = await _enrollmentRepository.GetAllAsync(
            filter: baseFilter,
            orderBy: orderBy,
            includeProperties: includes,
            page: queryParams.Page,
            pageSize: queryParams.PageSize
        );

        var expands2 = queryParams.Expand?.ToLower().Split(',') ?? [];
        return (enrollments.Select(e => MapToEnrollmentOfStudentResponse(e, expands2)), pagination);
    }

    public async Task<EnrollmentResponse> GetByIdAsync(int id, string? expand = null)
    {
        var includes = new List<string> { "Course" };

        if (!string.IsNullOrWhiteSpace(expand))
        {
            var expands = expand.ToLower().Split(',');
            if (expands.Contains("course"))
                includes.Add("Course.Semester");
        }

        var enrollment = await _enrollmentRepository.GetByIdAsync(id, includes)
            ?? throw new NotFoundException("Enrollment", id);

        var studentsMap = await FetchStudentsMapAsync(new[] { enrollment.StudentId });

        return MapToResponse(enrollment, studentsMap, expand);
    }

    public async Task<EnrollmentResponse> CreateAsync(EnrollmentRequest request)
    {
        // 1. Verify student exists via gRPC
        await VerifyStudentExistsAsync(request.StudentId);

        // 2. Create the enrollment
        var enrollment = new Enrollment
        {
            StudentId = request.StudentId,
            CourseId = request.CourseId,
            EnrollDate = DateTime.SpecifyKind(request.EnrollDate, DateTimeKind.Utc),
            Status = request.Status
        };

        try
        {
            await _enrollmentRepository.AddAsync(enrollment);
            await _enrollmentRepository.SaveAsync();
        }
        catch (Exception ex)
        {
            throw new BadRequestException("Could not create enrollment. It might already exist.", ex.Message);
        }

        var loaded = await _enrollmentRepository.GetByIdAsync(enrollment.EnrollmentId, new List<string> { "Course" });
        var studentsMap = await FetchStudentsMapAsync(new[] { enrollment.StudentId });

        await _publishEndpoint.Publish(new EnrollmentCreatedEvent
        {
            EnrollmentId = enrollment.EnrollmentId,
            StudentId = enrollment.StudentId,
            CourseId = enrollment.CourseId,
            CourseName = loaded?.Course?.CourseName,
            EnrollDate = enrollment.EnrollDate,
            Status = enrollment.Status
        });

        return MapToResponse(loaded ?? enrollment, studentsMap, "student,course");
    }

    public async Task UpdateAsync(int id, EnrollmentRequest request)
    {
        // 1. Verify student exists via gRPC
        await VerifyStudentExistsAsync(request.StudentId);

        // 2. Load enrollment
        var enrollment = await _enrollmentRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Enrollment", id);

        var oldStatus = enrollment.Status;

        enrollment.StudentId = request.StudentId;
        enrollment.CourseId = request.CourseId;
        enrollment.EnrollDate = DateTime.SpecifyKind(request.EnrollDate, DateTimeKind.Utc);
        enrollment.Status = request.Status;

        _enrollmentRepository.Update(enrollment);
        await _enrollmentRepository.SaveAsync();

        if (oldStatus != request.Status)
        {
            await _publishEndpoint.Publish(new EnrollmentStatusChangedEvent
            {
                EnrollmentId = enrollment.EnrollmentId,
                StudentId = enrollment.StudentId,
                CourseId = enrollment.CourseId,
                OldStatus = oldStatus,
                NewStatus = request.Status
            });
        }
    }

    public async Task DeleteAsync(int id)
    {
        var enrollment = await _enrollmentRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Enrollment", id);

        _enrollmentRepository.Delete(enrollment);
        await _enrollmentRepository.SaveAsync();
    }

    private EnrollmentResponse MapToResponse(Enrollment enrollment, Dictionary<int, GrpcStudentResponse> studentsMap, string? expand = null)
    {
        var hasStudent = studentsMap.TryGetValue(enrollment.StudentId, out var s);
        var response = new EnrollmentResponse
        {
            EnrollmentId = enrollment.EnrollmentId,
            StudentId = enrollment.StudentId,
            StudentName = hasStudent ? s!.FullName : null,
            CourseId = enrollment.CourseId,
            CourseName = enrollment.Course?.CourseName,
            EnrollDate = enrollment.EnrollDate,
            Status = enrollment.Status
        };

        if (!string.IsNullOrWhiteSpace(expand))
        {
            var expands = expand.ToLower().Split(',');

            if (expands.Contains("student") && hasStudent)
            {
                response.Student = new StudentBriefResponse
                {
                    StudentId = s!.StudentId,
                    StudentCode = s.StudentCode,
                    FullName = s.FullName,
                    Email = s.Email,
                    Phone = s.Phone,
                    DateOfBirth = DateTime.Parse(s.DateOfBirth)
                };
            }

            if (expands.Contains("course") && enrollment.Course != null)
            {
                response.Course = new CourseBriefResponse
                {
                    CourseId = enrollment.Course.CourseId,
                    CourseName = enrollment.Course.CourseName,
                    SemesterId = enrollment.Course.SemesterId,
                    SemesterName = enrollment.Course.Semester?.SemesterName,
                    EnrollmentCount = enrollment.Course.Enrollments?.Count ?? 0
                };
            }
        }

        return response;
    }

    private EnrollmentOfCourseResponse MapToEnrollmentOfCourseResponse(Enrollment enrollment, Dictionary<int, GrpcStudentResponse> studentsMap, string[] expands)
    {
        var hasStudent = studentsMap.TryGetValue(enrollment.StudentId, out var s);
        var response = new EnrollmentOfCourseResponse
        {
            EnrollmentId = enrollment.EnrollmentId,
            StudentId = enrollment.StudentId,
            StudentName = hasStudent ? s!.FullName : null,
            EnrollDate = enrollment.EnrollDate,
            Status = enrollment.Status
        };

        if (expands.Contains("student") && hasStudent)
        {
            response.Student = new StudentBriefResponse
            {
                StudentId = s!.StudentId,
                StudentCode = s.StudentCode,
                FullName = s.FullName,
                Email = s.Email,
                Phone = s.Phone,
                DateOfBirth = DateTime.Parse(s.DateOfBirth)
            };
        }

        return response;
    }

    private EnrollmentOfStudentResponse MapToEnrollmentOfStudentResponse(Enrollment enrollment, string[] expands)
    {
        var response = new EnrollmentOfStudentResponse
        {
            EnrollmentId = enrollment.EnrollmentId,
            CourseId = enrollment.CourseId,
            CourseName = enrollment.Course?.CourseName,
            EnrollDate = enrollment.EnrollDate,
            Status = enrollment.Status
        };

        if (expands.Contains("course") && enrollment.Course != null)
        {
            response.Course = new CourseBriefResponse
            {
                CourseId = enrollment.Course.CourseId,
                CourseName = enrollment.Course.CourseName,
                SemesterId = enrollment.Course.SemesterId,
                SemesterName = enrollment.Course.Semester?.SemesterName,
                EnrollmentCount = enrollment.Course.Enrollments?.Count ?? 0
            };
        }

        return response;
    }
}
