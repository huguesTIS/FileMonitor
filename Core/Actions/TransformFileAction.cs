namespace FileMonitor.Core.Actions;

public class TransformFileAction : IFileAction
{
    public string Name => "TransformFileAction";

    public async Task ExecuteAsync(FileRecord fileRecord, Stream? fileStream, CancellationToken cancellationToken)
    {
        if (fileStream == null)
        {
            throw new ArgumentNullException(nameof(fileStream));
        }

        using var transformedStream = new MemoryStream();
        using var reader = new StreamReader(fileStream);
        using var writer = new StreamWriter(transformedStream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (line != null)
            {
                await writer.WriteLineAsync(line.ToUpperInvariant()); // Exemple de transformation
            }
        }

        transformedStream.Seek(0, SeekOrigin.Begin);
        fileStream.Dispose(); // Remplacer l'ancien flux par le nouveau
        fileStream = transformedStream;
    }
}
