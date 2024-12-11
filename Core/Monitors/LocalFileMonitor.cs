// implemente deux strategie de deduplication
// - Gérer les doublons par fichier et par type d'événement.
// - Ajouter une période de déduplication pour ignorer les événements fréquents.
namespace FileMonitor.Core.Monitors;

public class LocalFileMonitor : IMonitor, IDisposable
{
    private FileSystemWatcher? _watcher;

    private readonly Job _job;
    private readonly IEventQueue _eventQueue;
    private readonly ILogger<LocalFileMonitor> _logger;
    private readonly ConcurrentDictionary<string, (string EventType, DateTime LastEventTime)> _eventCache = new();
    private readonly TimeSpan _eventDebounceTime = TimeSpan.FromMilliseconds(500); // 500ms de délai

    public LocalFileMonitor(
        Job job,
        IEventQueue eventQueue,
        ILogger<LocalFileMonitor> logger)
    {
        _job = job ?? throw new ArgumentNullException(nameof(job));
        _eventQueue = eventQueue ?? throw new ArgumentNullException(nameof(eventQueue));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrEmpty(_job.SourceDescriptor.Path))
        {
            throw new ArgumentException("Source path cannot be null or empty", nameof(job.SourceDescriptor.Path));
        }

        // Configuration du FileSystemWatcher
        _watcher = new FileSystemWatcher(_job.SourceDescriptor.Path)
        {
            EnableRaisingEvents = false,
            IncludeSubdirectories = _job.IncludeSubdirectories,
            Filter = _job.FileFilter
        };
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_watcher == null)
        {
            throw new InvalidOperationException("FileSystemWatcher is not initialized.");
        }

        _watcher.EnableRaisingEvents = true;
        _watcher.Created += OnFileEvent;
        _watcher.Changed += OnFileEvent;

        _logger.LogInformation("LocalFileMonitor started for path: {Path}", _job.SourceDescriptor.Path);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_watcher == null)
        {
            return Task.CompletedTask;
        }

        _watcher.EnableRaisingEvents = false;
        _watcher.Created -= OnFileEvent;
        _watcher.Changed -= OnFileEvent;

        _logger.LogInformation("LocalFileMonitor stopped.");
        return Task.CompletedTask;
    }

    public Task<bool> IsConnectedAsync()
    {
        bool isConnected = Directory.Exists(_job.SourceDescriptor.Path);
        return Task.FromResult(isConnected);
    }

    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath == null)
        {
            _logger.LogWarning("File event detected but FullPath is null.");
            return;
        }

        string key = e.FullPath;
        string newEventType = e.ChangeType.ToString();
        DateTime now = DateTime.UtcNow;

        if (_eventCache.TryGetValue(key, out var existingEvent))
        {
            // Déduplication basée sur le type d'événement et le délai
            if (existingEvent.EventType == newEventType && now - existingEvent.LastEventTime < _eventDebounceTime)
            {
                _logger.LogInformation("Duplicate or debounced event skipped: {FilePath} ({EventType})", e.FullPath, e.ChangeType);
                return;
            }
        }

        // Mettre à jour le cache avec le nouvel événement
        _eventCache[key] = (newEventType, now);

        // Lancer l'opération asynchrone pour traiter l'événement
        _ = Task.Run(async () =>
        {
            try
            {
                var fileRecord = new FileRecord
                {
                    FilePath = e.FullPath,
                    LastModified = File.GetLastWriteTime(e.FullPath),
                    Size = new FileInfo(e.FullPath).Length,
                    JobId = _job.Id
                };

                await _eventQueue.EnqueueAsync(fileRecord, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue file event: {FilePath}", e.FullPath);
            }
            finally
            {
                // Optionnel : Nettoyage pour éviter la croissance indéfinie
                if (now - _eventCache[key].LastEventTime > _eventDebounceTime)
                {
                    _eventCache.TryRemove(key, out _);
                }
            }
        });
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _watcher = null;
        GC.SuppressFinalize(this); // Empêche les types dérivés avec finaliseur d'exiger une implémentation explicite d'IDisposable
    }
}
