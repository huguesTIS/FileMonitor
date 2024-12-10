using FileMonitor.Core.Models;

namespace FileMonitor.Core.Queue;

public class FileProcessingQueue
{
    private readonly PriorityQueue<FileRecord> _queue = new();

    public void Enqueue(FileRecord file, int priority)
    {
        _queue.Enqueue(file, priority);
    }

    public FileRecord? Dequeue()
    {
        return _queue.Dequeue();
    }

    public int Count => _queue.Count;
}
