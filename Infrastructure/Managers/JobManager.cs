namespace FileMonitor.Infrastructure.Managers;

using System.Collections.Concurrent;

public class JobManager : IJobManager
{
    private readonly ConcurrentDictionary<Guid, Job> _jobs = new();

    public Task AddJobAsync(Job job, CancellationToken cancellationToken)
    {
        if (!_jobs.TryAdd(job.Id, job))
        {
            throw new InvalidOperationException($"Job with ID {job.Id} already exists.");
        }
        return Task.CompletedTask;
    }

    public Task RemoveJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        _jobs.TryRemove(jobId, out _);
        return Task.CompletedTask;
    }

    public Job? GetJob(Guid jobId)
    {
        _jobs.TryGetValue(jobId, out var job);
        return job;
    }

    public IEnumerable<Job> GetAllJobs()
    {
        return _jobs.Values;
    }

    public bool TryGetJob(Guid jobId, out Job? job)
    {
        return _jobs.TryGetValue(jobId, out job);
    }
}


