namespace AbGen.Api.Domain.Jobs;

public sealed class JobRecord
{
    private JobRecord() { }

    public Guid Id { get; private set; }
    public string JobType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public JobStatus Status { get; private set; }
    public int ProgressPercent { get; private set; }
    public string CurrentStep { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? AttemptStartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public List<JobEventRecord> Events { get; private set; } = [];

    public static JobRecord Create(string jobType, string payload, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobType);
        ArgumentNullException.ThrowIfNull(payload);
        if (jobType.Length > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(jobType), "Job type must be 64 characters or fewer.");
        }

        return new JobRecord
        {
            Id = Guid.NewGuid(),
            JobType = jobType,
            Payload = payload,
            Status = JobStatus.Queued,
            CurrentStep = "Queued",
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void MarkRunning(DateTimeOffset now)
    {
        EnsureStatus(JobStatus.Queued);
        Status = JobStatus.Running;
        AttemptCount++;
        StartedAt ??= now;
        AttemptStartedAt = now;
        CurrentStep = "Starting";
        UpdatedAt = now;
    }

    public void ReportProgress(int progressPercent, string currentStep, DateTimeOffset now)
    {
        EnsureStatus(JobStatus.Running);
        if (progressPercent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(progressPercent), "Progress must be between 0 and 100.");
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(currentStep);

        ProgressPercent = progressPercent;
        CurrentStep = currentStep;
        UpdatedAt = now;
    }

    public void MarkSucceeded(DateTimeOffset now)
    {
        EnsureStatus(JobStatus.Running);
        Status = JobStatus.Succeeded;
        AttemptStartedAt = null;
        ProgressPercent = 100;
        CurrentStep = "Completed";
        CompletedAt = now;
        UpdatedAt = now;
    }

    public void MarkFailed(string errorCode, string errorMessage, DateTimeOffset now)
    {
        EnsureStatus(JobStatus.Running);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        Status = JobStatus.Failed;
        AttemptStartedAt = null;
        CurrentStep = "Failed";
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        CompletedAt = now;
        UpdatedAt = now;
    }

    public void RequeueAfterInterruptedAttempt(DateTimeOffset now)
    {
        EnsureStatus(JobStatus.Running);
        Status = JobStatus.Queued;
        AttemptCount++;
        AttemptStartedAt = null;
        CurrentStep = "Recovered after an interrupted attempt";
        UpdatedAt = now;
    }

    public void AddEvent(JobEventLevel level, string code, string message, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        if (code.Length > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(code), "Event code must be 64 characters or fewer.");
        }
        if (message.Length > 2000)
        {
            throw new ArgumentOutOfRangeException(nameof(message), "Event message must be 2,000 characters or fewer.");
        }

        Events.Add(new JobEventRecord(Id, Events.Count, level, code, message, now));
        UpdatedAt = now;
    }

    private void EnsureStatus(JobStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException($"Cannot perform this transition on a job in the {Status} state.");
        }
    }
}
