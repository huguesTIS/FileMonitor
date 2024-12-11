namespace FileMonitor.Core.Models;

public class Job
{
    public Guid Id { get; set; } = Guid.NewGuid(); // Identifiant unique pour le Job
    public string? Description { get; set; }
    public FolderDescriptor SourceDescriptor { get; set; } = null!;
    public FolderDescriptor DestinationDescriptor { get; set; } = null!;
    public MonitorMode Mode { get; set; }
    public string FileFilter { get; set; } = "*.*";
    public bool IncludeSubdirectories { get; set; } = true;

    // Retry strategy
    public int MaxRetries { get; set; } = 3; // Default value
    public double BackoffFactor { get; set; } = 1000; // Default 1 second backoff exponentiel: (Math.Pow(2, fileRecord.RetryCount) * backoffFactor);
    // Liste d'actions dans le pipeline
    public List<IFileAction> PreProcessingActions { get; set; } = [];
    public List<IFileAction> ProcessingActions { get; set; } = [];
    public List<IFileAction> PostProcessingActions { get; set; } = [];
}
