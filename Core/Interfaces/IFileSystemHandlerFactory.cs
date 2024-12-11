namespace FileMonitor.Core.Interfaces;

public interface IFileSystemHandlerFactory
{
    IFileSystemHandler GetHandler(FolderDescriptor descriptor);
}
