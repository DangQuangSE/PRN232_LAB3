using MassTransit;
using PRN232.LMSSystem.Messages;

namespace PRN232.LMSSystem.StudentService.Consumers;

public class EnrollmentCreatedConsumer : IConsumer<EnrollmentCreatedEvent>
{
    private readonly ILogger<EnrollmentCreatedConsumer> _logger;

    public EnrollmentCreatedConsumer(ILogger<EnrollmentCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<EnrollmentCreatedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "[RabbitMQ] Enrollment created — StudentId={StudentId} enrolled in Course '{CourseName}' (CourseId={CourseId}), EnrollmentId={EnrollmentId}, Status={Status}",
            msg.StudentId, msg.CourseName, msg.CourseId, msg.EnrollmentId, msg.Status);
        return Task.CompletedTask;
    }
}
