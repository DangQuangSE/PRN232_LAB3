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
[Route("api/v{version:apiVersion}/enrollments")]
[Authorize]
[Produces("application/json", "application/xml")]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly IDataShaper<EnrollmentResponse> _dataShaper;

    public EnrollmentsController(IEnrollmentService enrollmentService, IDataShaper<EnrollmentResponse> dataShaper)
    {
        _enrollmentService = enrollmentService;
        _dataShaper = dataShaper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] QueryParameters queryParams)
    {
        var (data, pagination) = await _enrollmentService.GetAllAsync(queryParams);
        var shapedData = _dataShaper.ShapeData(data, queryParams.Fields);
        return Ok(ApiResponse<object>.SuccessResponse(shapedData, "Enrollments retrieved successfully", pagination));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, [FromQuery] QueryParameters queryParams)
    {
        var enrollment = await _enrollmentService.GetByIdAsync(id, queryParams.Expand);
        var shapedData = _dataShaper.ShapeData(enrollment, queryParams.Fields);
        return Ok(ApiResponse<object>.SuccessResponse(shapedData, "Enrollment retrieved successfully"));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EnrollmentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.ErrorResponse("Invalid request data", ModelState));

        var enrollment = await _enrollmentService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = enrollment.EnrollmentId },
            ApiResponse<EnrollmentResponse>.SuccessResponse(enrollment, "Enrollment created successfully"));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] EnrollmentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.ErrorResponse("Invalid request data", ModelState));

        await _enrollmentService.UpdateAsync(id, request);
        return Ok(ApiResponse<object>.SuccessResponse(null, "Enrollment updated successfully"));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        await _enrollmentService.DeleteAsync(id);
        return Ok(ApiResponse<object>.SuccessResponse(null, "Enrollment deleted successfully"));
    }
}
