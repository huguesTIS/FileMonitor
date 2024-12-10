namespace FileMonitor.Processing;

public class Worker
{
    private readonly FileProcessingQueue _queue;
    private readonly FileSystemHandlerFactory _handlerFactory;
    private readonly ILogger _logger;

    public Worker(FileProcessingQueue queue, FileSystemHandlerFactory handlerFactory, ILogger logger)
    {
        _queue = queue;
        _handlerFactory = handlerFactory;
        _logger = logger;
    }

    public async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var fileRecord = _queue.Dequeue();
            if (fileRecord == null) continue;

            var startTime = DateTime.Now;

            try
            {
                var sourceHandler = _handlerFactory.CreateHandler(fileRecord.SourcePath);
                var destinationHandler = _handlerFactory.CreateHandler(fileRecord.DestinationPath);

                _logger.LogInformation($"Processing file {fileRecord.FilePath}...");
                using var stream = await sourceHandler.OpenReadAsync(fileRecord.FilePath, cancellationToken);
                await destinationHandler.WriteAsync(fileRecord.DestinationPath, stream, cancellationToken);

                fileRecord.FinalStatus = "Success";
                _logger.LogInformation($"Processed file {fileRecord.FilePath} in {DateTime.Now - startTime}.");
            }
            catch (Exception ex)
            {
                fileRecord.FinalStatus = "Failed";
                _logger.LogError($"Error processing file {fileRecord.FilePath}: {ex.Message}");
            }
        }
    }
}
