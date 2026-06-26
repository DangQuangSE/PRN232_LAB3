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
[Route("api/v{version:apiVersion}/subjects")]
[Authorize]
[Produces("application/json", "application/xml")]
public class SubjectsController : ControllerBase
{
    private readonly ISubjectService _subjectService;
    private readonly IDataShaper<SubjectResponse> _dataShaper;

    public SubjectsController(ISubjectService subjectService, IDataShaper<SubjectResponse> _shaper)
    {
        _subjectService = subjectService;
        _dataShaper = _shaper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] QueryParameters queryParams)
    {
        var (data, pagination) = await _subjectService.GetAllAsync(queryParams);
        var shapedData = _dataShaper.ShapeData(data, queryParams.Fields);
        return Ok(ApiResponse<object>.SuccessResponse(shapedData, "Subjects retrieved successfully", pagination));
    }

    [HttpGet("{id:int}", Name = "GetSubjectById")]
    public async Task<IActionResult> GetById([FromRoute] int id, [FromQuery] QueryParameters queryParams)
    {
        var subject = await _subjectService.GetByIdAsync(id, queryParams.Expand);
        var shapedData = _dataShaper.ShapeData(subject, queryParams.Fields);
        return Ok(ApiResponse<object>.SuccessResponse(shapedData, "Subject retrieved successfully"));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SubjectRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.ErrorResponse("Invalid request data", ModelState));

        var subject = await _subjectService.CreateAsync(request);
        return CreatedAtRoute("GetSubjectById", new { id = subject.SubjectId },
            ApiResponse<SubjectResponse>.SuccessResponse(subject, "Subject created successfully"));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] SubjectRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.ErrorResponse("Invalid request data", ModelState));

        await _subjectService.UpdateAsync(id, request);
        return Ok(ApiResponse<object>.SuccessResponse(null, "Subject updated successfully"));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        await _subjectService.DeleteAsync(id);
        return Ok(ApiResponse<object>.SuccessResponse(null, "Subject deleted successfully"));
    }
}
