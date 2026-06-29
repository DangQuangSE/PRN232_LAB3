using MassTransit;
using PRN232.LMSSystem.Messages;

namespace PRN232.LMSSystem.StudentService.Consumers;

public class EnrollmentStatusChangedConsumer : IConsumer<EnrollmentStatusChangedEvent>
{
    private readonly ILogger<EnrollmentStatusChangedConsumer> _logger;

    public EnrollmentStatusChangedConsumer(ILogger<EnrollmentStatusChangedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<EnrollmentStatusChangedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "[RabbitMQ] Enrollment status changed — EnrollmentId={EnrollmentId}, StudentId={StudentId}, CourseId={CourseId}: {OldStatus} → {NewStatus}",
            msg.EnrollmentId, msg.StudentId, msg.CourseId, msg.OldStatus, msg.NewStatus);
        return Task.CompletedTask;
    }
}
