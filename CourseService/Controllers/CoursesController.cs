using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PRN232.LMSSystem.CourseService.Helpers;
using PRN232.LMSSystem.CourseService.Interfaces;
using PRN232.LMSSystem.CourseService.Models.Query;
using PRN232.LMSSystem.CourseService.Models.Request;
using PRN232.LMSSystem.CourseService.Models.Response;
using Asp.Versioning;

namespace PRN232.LMSSystem.CourseService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/courses")]
[Authorize]
[Produces("application/json", "application/xml")]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly IDataShaper<CourseResponse> _dataShaper;
    private readonly IEnrollmentService _enrollmentService;
    private readonly IDataShaper<EnrollmentOfCourseResponse> _enrollmentDataShaper;
    private readonly IDataShaper<StudentResponse> _studentDataShaper;

    public CoursesController(
        ICourseService courseService,
        IDataShaper<CourseResponse> dataShaper,
        IEnrollmentService enrollmentService,
        IDataShaper<EnrollmentOfCourseResponse> enrollmentDataShaper,
        IDataShaper<StudentResponse> studentDataShaper)
    {
        _courseService = courseService;
        _dataShaper = dataShaper;
        _enrollmentService = enrollmentService;
        _enrollmentDataShaper = enrollmentDataShaper;
        _studentDataShaper = studentDataShaper;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CourseResponse>>), 200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetAll([FromQuery] QueryParameters queryParams)
    {
        var (data, pagination) = await _courseService.GetAllAsync(queryParams);
        var shapedData = _dataShaper.ShapeData(data, queryParams.Fields);
        return Ok(ApiResponse<object>.SuccessResponse(shapedData, "Courses retrieved successfully", pagination));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CourseResponse>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetById([FromRoute] int id, [FromQuery] QueryParameters queryParams)
    {
        var course = await _courseService.GetByIdAsync(id, queryParams.Expand);
        var shapedData = _dataShaper.ShapeData(course, queryParams.Fields);
        return Ok(ApiResponse<object>.SuccessResponse(shapedData, "Course retrieved successfully"));
    }

    [HttpGet("{id:int}/enrollments")]
    public async Task<IActionResult> GetEnrollments([FromRoute] int id, [FromQuery] QueryParameters queryParams)
    {
        await _courseService.GetByIdAsync(id);
        var (data, pagination) = await _enrollmentService.GetByCourseIdAsync(id, queryParams);
        var shapedData = _enrollmentDataShaper.ShapeData(data, queryParams.Fields);
        return Ok(ApiResponse<object>.SuccessResponse(shapedData, "Course enrollments retrieved successfully", pagination));
    }

    [HttpGet("{courseId:int}/students")]
    public async Task<IActionResult> GetStudentsByCourse([FromRoute] int courseId, [FromQuery] QueryParameters queryParams)
    {
        await _courseService.GetByIdAsync(courseId);

        // Force expand=student to populate the Student property via batch gRPC
        queryParams.Expand = "student";
        var (enrollments, pagination) = await _enrollmentService.GetByCourseIdAsync(courseId, queryParams);
        
        var studentResponses = enrollments
            .Where(e => e.Student != null)
            .Select(e => new StudentResponse
            {
                StudentId = e.Student!.StudentId,
                StudentCode = e.Student.StudentCode,
                FullName = e.Student.FullName,
                Email = e.Student.Email,
                Phone = e.Student.Phone,
                DateOfBirth = e.Student.DateOfBirth
            })
            .ToList();

        var shapedData = _studentDataShaper.ShapeData(studentResponses, queryParams.Fields);
        return Ok(ApiResponse<object>.SuccessResponse(shapedData, "Course students retrieved successfully", pagination));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CourseResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Create([FromBody] CourseRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.ErrorResponse("Invalid request data", ModelState));

        var course = await _courseService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = course.CourseId },
            ApiResponse<CourseResponse>.SuccessResponse(course, "Course created successfully"));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] CourseRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.ErrorResponse("Invalid request data", ModelState));

        await _courseService.UpdateAsync(id, request);
        return Ok(ApiResponse<object>.SuccessResponse(null, "Course updated successfully"));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        await _courseService.DeleteAsync(id);
        return Ok(ApiResponse<object>.SuccessResponse(null, "Course deleted successfully"));
    }
}
