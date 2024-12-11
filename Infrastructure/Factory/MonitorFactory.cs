namespace FileMonitor.Infrastructure.Factory;

public class MonitorFactory(IServiceProvider serviceProvider, IJobManager jobManager)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IJobManager _jobManager = jobManager;

    public IMonitor CreateMonitor(Guid jobId)
    {
        if (!_jobManager.TryGetJob(jobId, out var job))
        {
            throw new ArgumentException($"No job found with ID {jobId}");
        }

        return job.SourceDescriptor switch
        {
            LocalFolderDescriptor local => new LocalFileMonitor(
                job,
                _serviceProvider.GetRequiredService<IEventQueue>(),
                _serviceProvider.GetRequiredService<ILogger<LocalFileMonitor>>()
            ),
            //SmbFolderDescriptor smb => new SmbFileMonitor(
            //    job,
            //    _serviceProvider.GetRequiredService<IEventQueue>(),
            //    _serviceProvider.GetRequiredService<IFileSystemHandlerFactory>(),
            //    _serviceProvider.GetRequiredService<ILogger<SmbFileMonitor>>()
            //),
            //SftpFolderDescriptor sftp => new SftpFileMonitor(
            //    job,
            //    _serviceProvider.GetRequiredService<IEventQueue>(),
            //    _serviceProvider.GetRequiredService<IFileSystemHandlerFactory>(),
            //    _serviceProvider.GetRequiredService<ILogger<SftpFileMonitor>>()
            //),
            _ => throw new NotSupportedException($"Unsupported folder descriptor type: {job.SourceDescriptor.GetType()}")
        };
    }
}

