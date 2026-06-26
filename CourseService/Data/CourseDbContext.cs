using Microsoft.EntityFrameworkCore;
using PRN232.LMSSystem.CourseService.Entities;

namespace PRN232.LMSSystem.CourseService.Data;

public class CourseDbContext : DbContext
{
    public CourseDbContext(DbContextOptions<CourseDbContext> options) : base(options)
    {
    }

    public DbSet<Semester> Semesters { get; set; } = null!;
    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<Subject> Subjects { get; set; } = null!;
    public DbSet<Enrollment> Enrollments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Semester>().ToTable("Semester");
        modelBuilder.Entity<Course>().ToTable("Course");
        modelBuilder.Entity<Subject>().ToTable("Subject");
        modelBuilder.Entity<Enrollment>().ToTable("Enrollment");

        modelBuilder.Entity<Semester>(entity =>
        {
            entity.HasKey(e => e.SemesterId);
            entity.Property(e => e.SemesterName).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseId);
            entity.Property(e => e.CourseName).IsRequired().HasMaxLength(100);
            
            entity.HasOne(d => d.Semester)
                .WithMany(p => p.Courses)
                .HasForeignKey(d => d.SemesterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.SubjectId);
            entity.Property(e => e.SubjectCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.SubjectName).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.EnrollmentId);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);

            entity.HasOne(d => d.Course)
                .WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => new { e.StudentId, e.CourseId }).IsUnique();
        });
    }
}
