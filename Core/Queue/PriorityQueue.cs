namespace FileMonitor.Core.Queue;

public class PriorityQueue<T>
{
    private readonly SortedDictionary<int, Queue<T>> _queues = new();

    public void Enqueue(T item, int priority)
    {
        if (!_queues.ContainsKey(priority))
        {
            _queues[priority] = new Queue<T>();
        }
        _queues[priority].Enqueue(item);
    }

    public T? Dequeue()
    {
        foreach (var priorityQueue in _queues)
        {
            if (priorityQueue.Value.Count > 0)
            {
                return priorityQueue.Value.Dequeue();
            }
        }
        return default;
    }

    public int Count => _queues.Sum(q => q.Value.Count);
}

