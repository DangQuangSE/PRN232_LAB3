using Microsoft.EntityFrameworkCore;
using PRN232.LMSSystem.CourseService.Entities;
using PRN232.LMSSystem.CourseService.Interfaces;
using PRN232.LMSSystem.CourseService.Exceptions;
using PRN232.LMSSystem.CourseService.Helpers;
using PRN232.LMSSystem.CourseService.Models.Query;
using PRN232.LMSSystem.CourseService.Models.Request;
using PRN232.LMSSystem.CourseService.Models.Response;
using PRN232.LMSSystem.Grpc;
using GrpcStudentResponse = PRN232.LMSSystem.Grpc.StudentResponse;
using System.Linq.Expressions;

namespace PRN232.LMSSystem.CourseService.Services;

public class CourseService : ICourseService
{
    private readonly ICourseRepository _courseRepository;
    private readonly StudentGrpc.StudentGrpcClient _grpcClient;

    public CourseService(ICourseRepository courseRepository, StudentGrpc.StudentGrpcClient grpcClient)
    {
        _courseRepository = courseRepository;
        _grpcClient = grpcClient;
    }

    public async Task<(IEnumerable<CourseResponse> Data, PaginationMetadata Pagination)> GetAllAsync(QueryParameters queryParams)
    {
        Expression<Func<Course, bool>>? filter = null;
        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var searchLower = queryParams.Search.ToLower().Trim();
            filter = c => c.CourseName.ToLower().Contains(searchLower);
        }

        int totalItems = await _courseRepository.CountAsync(filter);
        var pagination = new PaginationMetadata(queryParams.Page, queryParams.PageSize, totalItems);

        Func<IQueryable<Course>, IOrderedQueryable<Course>>? orderBy = null;
        if (!string.IsNullOrWhiteSpace(queryParams.Sort))
            orderBy = q => (IOrderedQueryable<Course>)QueryHelper.ApplySort(q, queryParams.Sort);
        else
            orderBy = q => q.OrderBy(c => c.CourseId);

        bool includeEnrollments = !string.IsNullOrWhiteSpace(queryParams.Expand) &&
            queryParams.Expand.ToLower().Split(',').Contains("enrollments");

        var courses = await _courseRepository.GetCoursesWithCountAsync(
            filter: filter,
            orderBy: orderBy,
            page: queryParams.Page,
            pageSize: queryParams.PageSize,
            includeEnrollments: includeEnrollments
        );

        var mappedCourses = new List<CourseResponse>();
        var allStudentIds = courses
            .SelectMany(c => c.Course.Enrollments.Select(e => e.StudentId))
            .Distinct()
            .ToList();

        // Query student details from StudentService via gRPC
        var studentMap = new Dictionary<int, GrpcStudentResponse>();
        if (includeEnrollments && allStudentIds.Any())
        {
            try
            {
                var grpcReq = new StudentsRequest();
                grpcReq.StudentIds.AddRange(allStudentIds);
                var grpcRes = await _grpcClient.GetStudentsByIdsAsync(grpcReq);
                foreach (var s in grpcRes.Students)
                {
                    studentMap[s.StudentId] = s;
                }
            }
            catch (Exception)
            {
                // Fallback: return courses without student names if StudentService is down
            }
        }

        foreach (var c in courses)
        {
            var res = MapToResponse(c, queryParams.Expand, studentMap);
            mappedCourses.Add(res);
        }

        return (mappedCourses, pagination);
    }

    public async Task<(IEnumerable<CourseBriefResponse> Data, PaginationMetadata Pagination)> GetBySemesterIdAsync(int semesterId, QueryParameters queryParams)
    {
        Expression<Func<Course, bool>> filter = c => c.SemesterId == semesterId;

        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var searchLower = queryParams.Search.ToLower().Trim();
            filter = c => c.SemesterId == semesterId && c.CourseName.ToLower().Contains(searchLower);
        }

        int totalItems = await _courseRepository.CountAsync(filter);
        var pagination = new PaginationMetadata(queryParams.Page, queryParams.PageSize, totalItems);

        Func<IQueryable<Course>, IOrderedQueryable<Course>>? orderBy = null;
        if (!string.IsNullOrWhiteSpace(queryParams.Sort))
            orderBy = q => (IOrderedQueryable<Course>)QueryHelper.ApplySort(q, queryParams.Sort);
        else
            orderBy = q => q.OrderBy(c => c.CourseId);

        var courses = await _courseRepository.GetCoursesWithCountAsync(
            filter: filter,
            orderBy: orderBy,
            page: queryParams.Page,
            pageSize: queryParams.PageSize
        );

        return (courses.Select(c => new CourseBriefResponse
        {
            CourseId = c.Course.CourseId,
            CourseName = c.Course.CourseName,
            SemesterId = c.Course.SemesterId,
            SemesterName = c.Course.Semester?.SemesterName,
            EnrollmentCount = c.EnrollmentCount
        }), pagination);
    }

    public async Task<CourseResponse> GetByIdAsync(int id, string? expand = null)
    {
        bool includeEnrollments = expand?.ToLower().Split(',').Contains("enrollments") ?? false;
        var courseWithCount = await _courseRepository.GetCourseWithCountByIdAsync(id, includeEnrollments)
            ?? throw new NotFoundException("Course", id);

        var studentMap = new Dictionary<int, GrpcStudentResponse>();
        if (includeEnrollments && courseWithCount.Course.Enrollments.Any())
        {
            var ids = courseWithCount.Course.Enrollments.Select(e => e.StudentId).Distinct().ToList();
            try
            {
                var grpcReq = new StudentsRequest();
                grpcReq.StudentIds.AddRange(ids);
                var grpcRes = await _grpcClient.GetStudentsByIdsAsync(grpcReq);
                foreach (var s in grpcRes.Students)
                {
                    studentMap[s.StudentId] = s;
                }
            }
            catch (Exception)
            {
            }
        }

        return MapToResponse(courseWithCount, expand, studentMap);
    }

    public async Task<CourseResponse> CreateAsync(CourseRequest request)
    {
        var course = new Course
        {
            CourseName = request.CourseName,
            SemesterId = request.SemesterId
        };

        await _courseRepository.AddAsync(course);
        await _courseRepository.SaveAsync();

        var loaded = await _courseRepository.GetCourseWithCountByIdAsync(course.CourseId);
        return MapToResponse(loaded ?? new CourseWithCount { Course = course, EnrollmentCount = 0 }, null, null);
    }

    public async Task UpdateAsync(int id, CourseRequest request)
    {
        var course = await _courseRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Course", id);

        course.CourseName = request.CourseName;
        course.SemesterId = request.SemesterId;

        _courseRepository.Update(course);
        await _courseRepository.SaveAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var course = await _courseRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Course", id);

        _courseRepository.Delete(course);
        await _courseRepository.SaveAsync();
    }

    private CourseResponse MapToResponse(CourseWithCount courseWithCount, string? expand, Dictionary<int, GrpcStudentResponse>? studentMap)
    {
        var course = courseWithCount.Course;

        var response = new CourseResponse
        {
            CourseId = course.CourseId,
            CourseName = course.CourseName,
            SemesterId = course.SemesterId,
            SemesterName = course.Semester?.SemesterName,
            EnrollmentCount = courseWithCount.EnrollmentCount
        };

        if (!string.IsNullOrWhiteSpace(expand))
        {
            var expands = expand.ToLower().Split(',');

            if (expands.Contains("semester") && course.Semester != null)
            {
                response.Semester = new SemesterBriefResponse
                {
                    SemesterId = course.Semester.SemesterId,
                    SemesterName = course.Semester.SemesterName,
                    StartDate = course.Semester.StartDate,
                    EndDate = course.Semester.EndDate,
                    CourseCount = course.Semester.Courses?.Count ?? 0
                };
            }

            if (expands.Contains("enrollments") && course.Enrollments != null)
            {
                response.Enrollments = course.Enrollments.Select(e =>
                {
                    var stdName = "";
                    if (studentMap != null && studentMap.TryGetValue(e.StudentId, out var s))
                    {
                        stdName = s.FullName;
                    }
                    return new CourseEnrollmentResponse
                    {
                        EnrollmentId = e.EnrollmentId,
                        StudentId = e.StudentId,
                        StudentName = stdName,
                        EnrollDate = e.EnrollDate,
                        Status = e.Status
                    };
                }).ToList();
            }
        }

        return response;
    }
}
