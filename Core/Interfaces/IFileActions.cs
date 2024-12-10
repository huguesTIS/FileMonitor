namespace FileMonitor.Core.Interfaces;

public interface IFileAction
{
    string Name { get; } // Nom descriptif de l'action
    Task ExecuteAsync(FileRecord fileRecord, Stream? fileStream, CancellationToken cancellationToken);
}
