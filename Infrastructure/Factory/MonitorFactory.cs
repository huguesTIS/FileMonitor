namespace FileMonitor.Infrastructure.Factory;

public class MonitorFactory
{
    private readonly IServiceProvider _serviceProvider;

    public MonitorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IMonitor CreateMonitor(FolderDescriptor descriptor)
    {
        return descriptor switch
        {
            LocalFolderDescriptor local => new LocalFileMonitor(
                local,
                _serviceProvider.GetRequiredService<IEventQueue>(),
                _serviceProvider.GetRequiredService<ILogger<LocalFileMonitor>>()
            ),
            SmbFolderDescriptor smb => new SmbFileMonitor(
                smb,
                _serviceProvider.GetRequiredService<IEventQueue>(),
                _serviceProvider.GetRequiredService<IFileSystemHandlerFactory>(),
                _serviceProvider.GetRequiredService<ILogger<SmbFileMonitor>>()
            ),
            SftpFolderDescriptor sftp => new SftpFileMonitor(
                sftp,
                _serviceProvider.GetRequiredService<IEventQueue>(),
                _serviceProvider.GetRequiredService<IFileSystemHandlerFactory>(),
                _serviceProvider.GetRequiredService<ILogger<SftpFileMonitor>>()
            ),
            _ => throw new NotSupportedException($"Unsupported folder descriptor type: {descriptor.GetType()}")
        };
    }
}
