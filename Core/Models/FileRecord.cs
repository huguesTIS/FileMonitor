namespace FileMonitor.Core.Models;

public class FileRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FilePath { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    public List<FileEvent> EventHistory { get; set; } = new();
    public string FinalStatus { get; set; } = "Pending"; // "Success", "Failed"
}
