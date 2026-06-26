using PRN232.LMSSystem.StudentService.Data;
using PRN232.LMSSystem.StudentService.Entities;
using PRN232.LMSSystem.StudentService.Interfaces;

namespace PRN232.LMSSystem.StudentService.Repositories;

public class StudentRepository : GenericRepository<Student>, IStudentRepository
{
    public StudentRepository(StudentDbContext context) : base(context)
    {
    }
}
