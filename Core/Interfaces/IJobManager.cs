namespace FileMonitor.Core.Interfaces;

public interface IJobManager
{
    Task AddJobAsync(Job job, CancellationToken cancellationToken);
    Task RemoveJobAsync(Guid jobId, CancellationToken cancellationToken);
    Job? GetJob(Guid jobId);
    IEnumerable<Job> GetAllJobs();
    bool TryGetJob(Guid jobId, out Job? job);
}
