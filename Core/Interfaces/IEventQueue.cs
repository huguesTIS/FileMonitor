namespace FileMonitor.Core.Interfaces;

public interface IEventQueue
{
    Task EnqueueAsync(FileRecord fileRecord, CancellationToken cancellationToken, int delayMs = 0);
    Task<FileRecord?> DequeueAsync(CancellationToken cancellationToken);
    int Count { get; }
}
