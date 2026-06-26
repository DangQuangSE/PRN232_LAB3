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
[Route("api/v{version:apiVersion}/semesters")]
[Authorize]
[Produces("application/json", "application/xml")]
public class SemestersController : ControllerBase
{
    private readonly ISemesterService _semesterService;
    private readonly IDataShaper<SemesterResponse> _dataShaper;

    public SemestersController(ISemesterService semesterService, IDataShaper<SemesterResponse> dataShaper)
    {
        _semesterService = semesterService;
        _dataShaper = dataShaper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] QueryParameters queryParams)
    {
        var (data, pagination) = await _semesterService.GetAllAsync(queryParams);
        var shapedData = _dataShaper.ShapeData(data, queryParams.Fields);
        return Ok(ApiResponse<object>.SuccessResponse(shapedData, "Semesters retrieved successfully", pagination));
    }

    [HttpGet("{id:int}", Name = "GetSemesterById")]
    public async Task<IActionResult> GetById([FromRoute] int id, [FromQuery] QueryParameters queryParams)
    {
        var semester = await _semesterService.GetByIdAsync(id, queryParams.Expand);
        var shapedData = _dataShaper.ShapeData(semester, queryParams.Fields);
        return Ok(ApiResponse<object>.SuccessResponse(shapedData, "Semester retrieved successfully"));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SemesterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.ErrorResponse("Invalid request data", ModelState));

        var semester = await _semesterService.CreateAsync(request);
        return CreatedAtRoute("GetSemesterById", new { id = semester.SemesterId },
            ApiResponse<SemesterResponse>.SuccessResponse(semester, "Semester created successfully"));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] SemesterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.ErrorResponse("Invalid request data", ModelState));

        await _semesterService.UpdateAsync(id, request);
        return Ok(ApiResponse<object>.SuccessResponse(null, "Semester updated successfully"));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        await _semesterService.DeleteAsync(id);
        return Ok(ApiResponse<object>.SuccessResponse(null, "Semester deleted successfully"));
    }
}
