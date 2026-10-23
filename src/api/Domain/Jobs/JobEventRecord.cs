namespace AbGen.Api.Domain.Jobs;

public sealed class JobEventRecord(
    Guid jobId,
    int sequenceNumber,
    JobEventLevel level,
    string code,
    string message,
    DateTimeOffset occurredAt)
{
    public Guid JobId { get; private set; } = jobId;
    public int SequenceNumber { get; private set; } = sequenceNumber;
    public JobEventLevel Level { get; private set; } = level;
    public string Code { get; private set; } = code;
    public string Message { get; private set; } = message;
    public DateTimeOffset OccurredAt { get; private set; } = occurredAt;
}
