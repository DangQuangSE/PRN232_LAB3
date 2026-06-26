using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PRN232.LMSSystem.CourseService.Helpers;
using PRN232.LMSSystem.CourseService.Interfaces;
using PRN232.LMSSystem.CourseService.Models.Query;
using PRN232.LMSSystem.CourseService.Models.Response;
using PRN232.LMSSystem.Grpc;
using Asp.Versioning;

namespace PRN232.LMSSystem.CourseService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/students")]
[Authorize]
[Produces("application/json", "application/xml")]
public class StudentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly IDataShaper<EnrollmentOfStudentResponse> _dataShaper;
    private readonly StudentGrpc.StudentGrpcClient _grpcClient;

    public StudentsController(
        IEnrollmentService enrollmentService,
        IDataShaper<EnrollmentOfStudentResponse> dataShaper,
        StudentGrpc.StudentGrpcClient grpcClient)
    {
        _enrollmentService = enrollmentService;
        _dataShaper = dataShaper;
        _grpcClient = grpcClient;
    }

    [HttpGet("{id:int}/enrollments")]
    public async Task<IActionResult> GetEnrollments([FromRoute] int id, [FromQuery] QueryParameters queryParams)
    {
        // 1. Verify student existence via gRPC
        try
        {
            var verifyRes = await _grpcClient.VerifyStudentAsync(new StudentRequest { StudentId = id });
            if (!verifyRes.Exists)
            {
                return NotFound(ApiResponse<object>.ErrorResponse($"Student with ID '{id}' was not found."));
            }
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Could not verify student existence via gRPC. " + ex.Message));
        }

        // 2. Fetch the enrollments
        var (data, pagination) = await _enrollmentService.GetByStudentIdAsync(id, queryParams);
        var shapedData = _dataShaper.ShapeData(data, queryParams.Fields);
        return Ok(ApiResponse<object>.SuccessResponse(shapedData, "Student enrollments retrieved successfully", pagination));
    }
}
