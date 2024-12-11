namespace FileMonitor.Processing;

public class WorkerService : BackgroundService
{
    private readonly IEventQueue _eventQueue;
    private readonly IJobManager _jobManager;
    private readonly IFileSystemHandlerFactory _fileSystemHandlerFactory;
    private readonly ILogger<WorkerService> _logger;
    private readonly SemaphoreSlim _workerSemaphore;
    private readonly object _workerLock = new();

    private int _activeWorkers = 0;
    private const int MaxWorkers = 10;
    private const int QueueThresholdPerWorker = 10;
    private const int BatchSizeThreshold = 5; // Taille minimale pour former un batch

    public WorkerService(
        IEventQueue eventQueue,
        IJobManager jobManager,
        IFileSystemHandlerFactory fileSystemHandlerFactory,
        ILogger<WorkerService> logger)
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

        StartWorker(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await AdjustWorkerCountAsync();
            await Task.Delay(1000, stoppingToken);
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
                // Obtenez un batch de fichiers ou un seul fichier
                var batch = await GetBatchAsync(stoppingToken);

                if (batch.Count != 0)
                {
                    var worker = new Worker(
                        batch,
                        _jobManager,
                        _fileSystemHandlerFactory,
                        _eventQueue,
                        _logger
                    );

                    await worker.ProcessBatchAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
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

    private async Task<List<FileRecord>> GetBatchAsync(CancellationToken cancellationToken)
    {
        var batch = new List<FileRecord>();
        var fileEventGroups = new Dictionary<string, FileRecord>();

        while (batch.Count < BatchSizeThreshold && _eventQueue.Count > 0)
        {
            var fileRecord = await _eventQueue.DequeueAsync(cancellationToken);
            if (fileRecord == null) continue;

            // Regrouper par clé unique (par exemple, FilePath)
            if (fileEventGroups.TryGetValue(fileRecord.FilePath, out var existingRecord))
            {
                // Déduplication basée sur la dernière modification
                if (fileRecord.LastModified > existingRecord.LastModified)
                {
                    fileEventGroups[fileRecord.FilePath] = fileRecord;
                }
            }
            else
            {
                fileEventGroups[fileRecord.FilePath] = fileRecord;
            }
        }

        // Ajouter les résultats dédupliqués au batch
        batch.AddRange(fileEventGroups.Values);

        return batch;
    }


    private async Task AdjustWorkerCountAsync()
    {
        var queueCount = _eventQueue.Count;

        lock (_workerLock)
        {
            var targetWorkers = Math.Min(MaxWorkers, (queueCount / QueueThresholdPerWorker) + 1);

            while (_activeWorkers < targetWorkers)
            {
                StartWorker(CancellationToken.None);
            }
        }

        await Task.CompletedTask;
    }
}

