namespace FileMonitor.Infrastructure.Factory;

public class FileSystemHandlerFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<string, IFileSystemHandler> _handlerCache = new();

    public FileSystemHandlerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IFileSystemHandler CreateHandler(object descriptor)
    {
        string cacheKey = GenerateCacheKey(descriptor);

        return _handlerCache.GetOrAdd(cacheKey, _ => CreateNewHandler(descriptor));
    }

    private string GenerateCacheKey(object descriptor)
    {
        return descriptor switch
        {
            SftpFolderDescriptor sftp => $"sftp:{sftp.Host}:{sftp.Port}:{sftp.Username}",
            SmbFolderDescriptor smb => $"smb:{smb.UncPath}:{smb.Username}",
            LocalFolderDescriptor local => $"local:{local.Path}",
            _ => throw new ArgumentException($"Unsupported descriptor type: {descriptor.GetType()}")
        };
    }

    private IFileSystemHandler CreateNewHandler(object descriptor)
    {
        return descriptor switch
        {
            SftpFolderDescriptor sftp => _serviceProvider.GetRequiredService<SftpFileSystemHandler>(),
            SmbFolderDescriptor smb => _serviceProvider.GetRequiredService<SmbFileSystemHandler>(),
            LocalFolderDescriptor local => _serviceProvider.GetRequiredService<LocalFileSystemHandler>(),
            _ => throw new ArgumentException($"Unsupported descriptor type: {descriptor.GetType()}")
        };
    }
}



