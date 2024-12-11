namespace FileMonitor.Core.Actions;

public class LogFileAction(ILogger<LogFileAction> logger) : IFileAction
{
    private readonly ILogger<LogFileAction> _logger = logger;

    public string Name => "LogFileAction";

    public Task ExecuteAsync(FileRecord fileRecord, Stream? fileStream, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Processing file: {fileRecord.Path}, Size: {fileRecord.Size}, Status: {fileRecord.Status}");
        return Task.CompletedTask;
    }
}
