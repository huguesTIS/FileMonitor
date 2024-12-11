namespace FileMonitor.Processing;

public class WorkerServiceold : BackgroundService
{
    private readonly IEventQueue _eventQueue;
    private readonly IJobManager _jobManager;
    private readonly IFileSystemHandlerFactory _fileSystemHandlerFactory;
    private readonly ILogger<WorkerServiceold> _logger;
    private readonly SemaphoreSlim _workerSemaphore;
    private readonly object _workerLock = new();

    private int _activeWorkers = 0;
    private const int MaxWorkers = 10; // Nombre maximum de workers actifs
    private const int QueueThresholdPerWorker = 10; // Taille de queue par worker avant d'ajouter un nouveau worker

    public WorkerServiceold(
        IEventQueue eventQueue,
        IJobManager jobManager,
        IFileSystemHandlerFactory fileSystemHandlerFactory,
        ILogger<WorkerServiceold> logger)
    {
        _eventQueue = eventQueue ?? throw new ArgumentNullException(nameof(eventQueue));
        _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
        _fileSystemHandlerFactory = fileSystemHandlerFactory ?? throw new ArgumentNullException(nameof(fileSystemHandlerFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _workerSemaphore = new SemaphoreSlim(MaxWorkers);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WorkerService started.");

        // Lancer le premier worker
        StartWorker(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await AdjustWorkerCountAsync();
            await Task.Delay(1000, stoppingToken); // Vérification toutes les secondes
        }

        _logger.LogInformation("WorkerService stopping.");
    }

    private void StartWorker(CancellationToken stoppingToken)
    {
        lock (_workerLock)
        {
            if (_activeWorkers >= MaxWorkers)
            {
                _logger.LogWarning("Max worker limit reached. No new workers will be started.");
                return;
            }

            _activeWorkers++;
            _logger.LogInformation("Starting new worker. Active workers: {ActiveWorkers}", _activeWorkers);

            Task.Run(() => WorkerLoopAsync(stoppingToken), stoppingToken);
        }
    }

    private async Task WorkerLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var fileRecord = await _eventQueue.DequeueAsync(stoppingToken);
                if (fileRecord != null)
                {
                    await ProcessFileRecordAsync(fileRecord, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break; // Arrêter le worker
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in worker loop.");
            }
        }

        lock (_workerLock)
        {
            _activeWorkers--;
            _logger.LogInformation("Worker stopped. Active workers: {ActiveWorkers}", _activeWorkers);
        }
    }

    private async Task AdjustWorkerCountAsync()
    {
        var queueCount = _eventQueue.Count;

        lock (_workerLock)
        {
            var targetWorkers = Math.Min(MaxWorkers, queueCount / QueueThresholdPerWorker + 1);

            while (_activeWorkers < targetWorkers)
            {
                StartWorker(CancellationToken.None);
            }
        }

        await Task.CompletedTask;
    }

    private async Task ProcessFileRecordAsync(FileRecord fileRecord, CancellationToken cancellationToken)
    {
        var job = _jobManager.GetJob(fileRecord.JobId);
        if (job == null)
        {
            _logger.LogError("Job not found for JobId: {JobId}", fileRecord.JobId);
            return;
        }

        var sourceHandler = _fileSystemHandlerFactory.GetHandler(job.SourceDescriptor);
        var destinationHandler = _fileSystemHandlerFactory.GetHandler(job.DestinationDescriptor);

        try
        {
            _logger.LogInformation("Processing file: {FilePath} for JobId: {JobId}", fileRecord.FilePath, fileRecord.JobId);

            // Pre-Processing Actions
            foreach (var action in job.PreProcessingActions)
            {
                _logger.LogInformation("Executing PreProcessing action: {Action} for FilePath: {FilePath}", action.GetType().Name, fileRecord.FilePath);
                await action.ExecuteAsync(fileRecord, null, cancellationToken);
            }

            // Main Processing
            await using var sourceStream = await sourceHandler.OpenReadAsync(fileRecord.FilePath, cancellationToken);
            await using var destinationStream = new MemoryStream();

            foreach (var action in job.ProcessingActions)
            {
                _logger.LogInformation("Executing Processing action: {Action} for FilePath: {FilePath}", action.GetType().Name, fileRecord.FilePath);
                await action.ExecuteAsync(fileRecord, sourceStream, cancellationToken);
            }

            await destinationHandler.WriteAsync(fileRecord.FilePath, destinationStream, cancellationToken);

            // Post-Processing Actions
            foreach (var action in job.PostProcessingActions)
            {
                _logger.LogInformation("Executing PostProcessing action: {Action} for FilePath: {FilePath}", action.GetType().Name, fileRecord.FilePath);
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
