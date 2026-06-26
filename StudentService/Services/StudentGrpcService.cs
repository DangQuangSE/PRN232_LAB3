using Grpc.Core;
using PRN232.LMSSystem.Grpc;
using PRN232.LMSSystem.StudentService.Interfaces;

namespace PRN232.LMSSystem.StudentService.Services;

public class StudentGrpcService : StudentGrpc.StudentGrpcBase
{
    private readonly IStudentRepository _studentRepository;

    public StudentGrpcService(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    public override async Task<StudentResponse> GetStudentById(StudentRequest request, ServerCallContext context)
    {
        var student = await _studentRepository.GetByIdAsync(request.StudentId);
        if (student == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Student with ID {request.StudentId} not found"));
        }

        return new StudentResponse
        {
            StudentId = student.StudentId,
            StudentCode = student.StudentCode,
            FullName = student.FullName,
            Email = student.Email,
            Phone = student.Phone,
            DateOfBirth = student.DateOfBirth.ToString("o")
        };
    }

    public override async Task<StudentsResponse> GetStudentsByIds(StudentsRequest request, ServerCallContext context)
    {
        var ids = request.StudentIds.ToList();
        var students = await _studentRepository.GetAllAsync(s => ids.Contains(s.StudentId));
        
        var response = new StudentsResponse();
        response.Students.AddRange(students.Select(student => new StudentResponse
        {
            StudentId = student.StudentId,
            StudentCode = student.StudentCode,
            FullName = student.FullName,
            Email = student.Email,
            Phone = student.Phone,
            DateOfBirth = student.DateOfBirth.ToString("o")
        }));

        return response;
    }

    public override async Task<VerifyResponse> VerifyStudent(StudentRequest request, ServerCallContext context)
    {
        var student = await _studentRepository.GetByIdAsync(request.StudentId);
        return new VerifyResponse
        {
            Exists = student != null
        };
    }
}
