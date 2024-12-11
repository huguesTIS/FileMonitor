namespace FileMonitor.Core.Queue;

public class FileActionPipeline
{
    private readonly List<IFileAction> _actions = [];

    public void AddAction(IFileAction action)
    {
        _actions.Add(action);
    }

    public async Task ExecuteAsync(FileRecord fileRecord, Stream? fileStream, CancellationToken cancellationToken)
    {
        foreach (var action in _actions)
        {
            try
            {
                await action.ExecuteAsync(fileRecord, fileStream, cancellationToken);
            }
            catch (Exception ex)
            {
                // Gérer les erreurs pour chaque action
                Console.WriteLine($"Error in action {action.Name}: {ex.Message}");
                throw;
            }
        }
    }
}
