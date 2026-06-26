using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PRN232.LMSSystem.StudentService.Helpers;
using PRN232.LMSSystem.StudentService.Interfaces;
using PRN232.LMSSystem.StudentService.Models.Query;
using PRN232.LMSSystem.StudentService.Models.Request;
using PRN232.LMSSystem.StudentService.Models.Response;
using Asp.Versioning;

namespace PRN232.LMSSystem.StudentService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/students")]
[Authorize]
[Produces("application/json", "application/xml")]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;
    private readonly IDataShaper<StudentResponse> _dataShaper;
    private readonly ILogger<StudentsController> _logger;

    public StudentsController(
        IStudentService studentService,
        IDataShaper<StudentResponse> dataShaper,
        ILogger<StudentsController> logger)
    {
        _studentService = studentService;
        _dataShaper = dataShaper;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<StudentResponse>>), 200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetAll([FromQuery] StudentQueryRequest request)
    {
        var (data, pagination) = await _studentService.GetAllAsync(request);
        var shapedData = _dataShaper.ShapeData(data, request.Fields);
        return Ok(ApiResponse<object>.SuccessResponse(shapedData, "Students retrieved successfully", pagination));
    }

    [HttpGet("{id:int}", Name = "GetStudentById")]
    [ProducesResponseType(typeof(ApiResponse<StudentResponse>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetById([FromRoute] int id, [FromQuery] QueryParameters queryParams)
    {
        var student = await _studentService.GetByIdAsync(id, queryParams.Expand);
        var shapedData = _dataShaper.ShapeData(student, queryParams.Fields);
        return Ok(ApiResponse<object>.SuccessResponse(shapedData, "Student retrieved successfully"));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<StudentResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Create(
        [FromBody] CreateStudentRequest request, 
        [FromHeader(Name = "X-Request-Id")] string? requestId)
    {
        if (requestId != null)
        {
            _logger.LogInformation("Processing create student request with X-Request-Id: {RequestId}", requestId);
        }

        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.ErrorResponse("Invalid request data", ModelState));

        var student = await _studentService.CreateAsync(request);
        return CreatedAtRoute("GetStudentById", new { id = student.StudentId, version = "1" },
            ApiResponse<StudentResponse>.SuccessResponse(student, "Student created successfully"));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] CreateStudentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.ErrorResponse("Invalid request data", ModelState));

        await _studentService.UpdateAsync(id, request);
        return Ok(ApiResponse<object>.SuccessResponse(null, "Student updated successfully"));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        await _studentService.DeleteAsync(id);
        return Ok(ApiResponse<object>.SuccessResponse(null, "Student deleted successfully"));
    }
}
