namespace FileMonitor.Core.Interfaces;

public interface IMonitor : IDisposable
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    Task<bool> IsConnectedAsync();
}