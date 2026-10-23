using AbGen.Api.Domain.Jobs;
using AbGen.Api.Application.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AbGen.Api.Infrastructure.Jobs;

public sealed class JobProcessingService(
    IServiceScopeFactory scopeFactory,
    ISystemClock clock,
    ILogger<JobProcessingService> logger) : BackgroundService
{
    private static readonly TimeSpan RecoveryCheckInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RecoverInterruptedJobsAsync(stoppingToken);
            using var recoveryTimer = new PeriodicTimer(RecoveryCheckInterval);
            await ProcessPendingJobsAsync(recoveryTimer, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "The job processing service stopped unexpectedly.");
            throw;
        }
    }

    private async Task RecoverInterruptedJobsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IJobRepository>();
            await repository.RequeueInterruptedAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("Unable to recover interrupted jobs.", exception);
        }
    }

    private async Task ProcessPendingJobsAsync(PeriodicTimer recoveryTimer, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IJobRepository>();
            var queuedJob = await repository.GetNextQueuedAsync(cancellationToken);
            var processors = scope.ServiceProvider.GetServices<IJobProcessor>()
                .ToDictionary(processor => processor.JobType, StringComparer.OrdinalIgnoreCase);

            if (queuedJob is null)
            {
                if (await recoveryTimer.WaitForNextTickAsync(cancellationToken))
                {
                    await RecoverInterruptedJobsAsync(cancellationToken);
                }

                continue;
            }

            if (!processors.TryGetValue(queuedJob.JobType, out var processor))
            {
                logger.LogError("No processor is registered for job type {JobType}.", queuedJob.JobType);
                if (!await repository.TryBeginAttemptAsync(queuedJob.Id, cancellationToken))
                {
                    continue;
                }

                var unhandledJob = await repository.GetForProcessingAsync(queuedJob.Id, cancellationToken);
                if (unhandledJob is null)
                {
                    continue;
                }

                unhandledJob.MarkFailed("processor.not_found", $"No processor is registered for job type '{unhandledJob.JobType}'.", clock.UtcNow);
                unhandledJob.AddEvent(JobEventLevel.Error, "processor.not_found", unhandledJob.ErrorMessage!, clock.UtcNow);
                await repository.UpdateAsync(unhandledJob, cancellationToken);
                continue;
            }

            await ProcessJobAsync(repository, processor, queuedJob.Id, cancellationToken);
        }
    }

    private async Task ProcessJobAsync(
        IJobRepository repository,
        IJobProcessor processor,
        Guid jobId,
        CancellationToken stoppingToken)
    {
        if (!await repository.TryBeginAttemptAsync(jobId, stoppingToken))
        {
            return;
        }

        var job = await repository.GetForProcessingAsync(jobId, stoppingToken);
        if (job is null)
        {
            return;
        }

        logger.LogInformation("Processing job {JobId} ({JobType}), attempt {AttemptCount}.", job.Id, job.JobType, job.AttemptCount);

        try
        {
            await processor.ProcessAsync(job, async (progress, cancellationToken) =>
            {
                job.ReportProgress(progress.ProgressPercent, progress.CurrentStep, clock.UtcNow);
                await repository.UpdateAsync(job, cancellationToken);
                job.AddEvent(progress.Level, progress.Code, progress.Message, clock.UtcNow);
            }, stoppingToken);

            await repository.UpdateAsync(job, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Job {JobId} failed.", job.Id);
            job.MarkFailed("job.processing_failed", "The placeholder job could not be completed. Check server logs for details.", clock.UtcNow);
            job.AddEvent(JobEventLevel.Error, "job.processing_failed", job.ErrorMessage!, clock.UtcNow);
            await repository.UpdateAsync(job, CancellationToken.None);
        }
    }
}
