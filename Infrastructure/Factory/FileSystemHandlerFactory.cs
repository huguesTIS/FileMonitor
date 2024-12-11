namespace FileMonitor.Infrastructure.Factory;

public class FileSystemHandlerFactory : IFileSystemHandlerFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<string, IFileSystemHandler> _handlerCache = new();

    public FileSystemHandlerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IFileSystemHandler GetHandler(FolderDescriptor descriptor)
    {
        string cacheKey = GenerateCacheKey(descriptor);

        return _handlerCache.GetOrAdd(cacheKey, _ => CreateNewHandler(descriptor));
    }

    private static string GenerateCacheKey(FolderDescriptor descriptor)
    {
        return descriptor switch
        {
            SftpFolderDescriptor sftp => $"sftp:{sftp.Host}:{sftp.Port}:{sftp.Username}",
            SmbFolderDescriptor smb => $"smb:{smb.UncPath}:{smb.Username}",
            LocalFolderDescriptor local => $"local:{local.Path}",
            _ => throw new ArgumentException($"Unsupported descriptor type: {descriptor.GetType()}")
        };
    }

    private IFileSystemHandler CreateNewHandler(FolderDescriptor descriptor)
    {
        return descriptor switch
        {
            SftpFolderDescriptor => _serviceProvider.GetRequiredService<SftpFileSystemHandler>(),
            SmbFolderDescriptor => _serviceProvider.GetRequiredService<SmbFileSystemHandler>(),
            LocalFolderDescriptor => _serviceProvider.GetRequiredService<LocalFileSystemHandler>(),
            _ => throw new ArgumentException($"Unsupported descriptor type: {descriptor.GetType()}")
        };
    }
}
