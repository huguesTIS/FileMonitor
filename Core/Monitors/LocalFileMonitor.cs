using FileMonitor.Core.Interfaces;
using FileMonitor.Core.Models;
using FileMonitor.Core.Queue;

namespace FileMonitor.Core.Monitors;

public class LocalFileMonitor : IMonitor
{
    private FileSystemWatcher _watcher;
    private readonly LocalFolderDescriptor _descriptor;
    private readonly IEventQueue _eventQueue;
    private readonly ILogger<LocalFileMonitor> _logger;

    public LocalFileMonitor(
        LocalFolderDescriptor descriptor,
        IEventQueue eventQueue,
        ILogger<LocalFileMonitor> logger)
    {
        _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        _eventQueue = eventQueue ?? throw new ArgumentNullException(nameof(eventQueue));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _watcher = new FileSystemWatcher(_descriptor.Path)
        {
            EnableRaisingEvents = false,
            IncludeSubdirectories = true
        };
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _watcher.EnableRaisingEvents = true;
        _watcher.Created += OnFileEvent;
        _watcher.Changed += OnFileEvent;

        _logger.LogInformation($"LocalFileMonitor started for path: {_descriptor.Path}");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _watcher.EnableRaisingEvents = false;
        _watcher.Created -= OnFileEvent;
        _watcher.Changed -= OnFileEvent;

        _logger.LogInformation("LocalFileMonitor stopped.");
        return Task.CompletedTask;
    }

    public Task<bool> IsConnectedAsync()
    {
        return Task.FromResult(Directory.Exists(_descriptor.Path));
    }

    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        _logger.LogInformation($"File event detected: {e.FullPath} ({e.ChangeType})");

        var fileEvent = new FileRecord
        {
            FilePath = e.FullPath,
            LastModified = File.GetLastWriteTime(e.FullPath),
            DescriptorKey = $"local:{_descriptor.Path}",
            Size = new FileInfo(e.FullPath).Length
        };

        _eventQueue.Enqueue(fileEvent);
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _watcher = null;
    }
}

