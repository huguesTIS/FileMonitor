namespace FileMonitor.Processing
{
    public class Worker
    {
        private readonly List<FileRecord> _fileRecords;
        private readonly IJobManager _jobManager;
        private readonly IFileSystemHandlerFactory _fileSystemHandlerFactory;
        private readonly ILogger<WorkerService> _logger;
        private readonly IEventQueue _eventQueue;

        public Worker(
            List<FileRecord> fileRecords,
            IJobManager jobManager,
            IFileSystemHandlerFactory fileSystemHandlerFactory,
            IEventQueue eventQueue,
            ILogger<WorkerService> logger)
        {
            _fileRecords = fileRecords;
            _jobManager = jobManager;
            _fileSystemHandlerFactory = fileSystemHandlerFactory;
            _eventQueue = eventQueue ?? throw new ArgumentNullException(nameof(eventQueue));
            _logger = logger;
        }

        public async Task ProcessBatchAsync(CancellationToken cancellationToken)
        {
            foreach (var fileRecord in _fileRecords)
            {
                try
                {
                    var job = _jobManager.GetJob(fileRecord.JobId);
                    if (job == null)
                    {
                        _logger.LogError("Job not found for JobId: {JobId}", fileRecord.JobId);
                        continue;
                    }

                    var sourceHandler = _fileSystemHandlerFactory.GetHandler(job.SourceDescriptor);
                    var destinationHandler = _fileSystemHandlerFactory.GetHandler(job.DestinationDescriptor);

                    // Pre-processing actions
                    foreach (var action in job.PreProcessingActions)
                    {
                        await action.ExecuteAsync(fileRecord, null, cancellationToken);
                    }

                    // Main processing
                    await using var sourceStream = await sourceHandler.OpenReadAsync(fileRecord.FilePath, cancellationToken);
                    await using var destinationStream = new MemoryStream();

                    foreach (var action in job.ProcessingActions)
                    {
                        await action.ExecuteAsync(fileRecord, sourceStream, cancellationToken);
                    }

                    await destinationHandler.WriteAsync(fileRecord.FilePath, destinationStream, cancellationToken);

                    // Post-processing actions
                    foreach (var action in job.PostProcessingActions)
                    {
                        await action.ExecuteAsync(fileRecord, null, cancellationToken);
                    }

                    fileRecord.FinalStatus = "Success";
                    _logger.LogInformation("File processed successfully: {FilePath}", fileRecord.FilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing file: {FilePath}", fileRecord.FilePath);
                    await HandleFailureAsync(fileRecord, cancellationToken);
                }
            }
        }

        private async Task HandleFailureAsync(FileRecord fileRecord, CancellationToken cancellationToken)
        {
            var job = _jobManager.GetJob(fileRecord.JobId);
            if (job == null)
            {
                _logger.LogError("Job not found for JobId: {JobId}", fileRecord.JobId);
                return;
            }

            // Récupérer les valeurs des paramètres (priorité au FolderDescriptor)
            var sourceRetries = job.SourceDescriptor.MaxRetries ?? job.MaxRetries;
            var sourceBackoff = job.SourceDescriptor.BackoffFactor ?? job.BackoffFactor;

            fileRecord.RetryCount++;
            if (fileRecord.RetryCount > sourceRetries)
            {
                fileRecord.FinalStatus = "Failed";
                _logger.LogError("Max retries reached for FilePath: {FilePath}. Marking as failed.", fileRecord.FilePath);
                return;
            }

            // Calcul du délai avec backoff exponentiel
            var delay = (int)(Math.Pow(2, fileRecord.RetryCount) * sourceBackoff);
            _logger.LogWarning("Retrying FilePath: {FilePath} after {Delay}ms (Retry {RetryCount})", fileRecord.FilePath, delay, fileRecord.RetryCount);

            // Réenquêter avec délai
            await _eventQueue.EnqueueAsync(fileRecord, cancellationToken, delay);
        }
    }
}


