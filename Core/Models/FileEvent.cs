namespace FileMonitor.Core.Models;

public class FileEvent
{
    public DateTime EventTime { get; set; } = DateTime.Now;
    public string EventType { get; set; } = string.Empty; // Created, Processed, Failed, etc.
    public string Status { get; set; } = "Pending"; // Pending, Processed, Failed
    public string? Error { get; set; }
    public TimeSpan Duration { get; set; }
}
