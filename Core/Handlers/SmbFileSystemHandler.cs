namespace FileMonitor.Core.Handlers;

public class SmbFileSystemHandler : IFileSystemHandler
{
    private readonly SmbFolderDescriptor _descriptor;
    private readonly IImpersonationService _impersonationService;

    public SmbFileSystemHandler(SmbFolderDescriptor descriptor, IImpersonationService impersonationService)
    {
        _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        _impersonationService = impersonationService ?? throw new ArgumentNullException(nameof(impersonationService));
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken)
    {
        if (ShouldUseImpersonation())
        {
            await _impersonationService.RunImpersonatedAsync(
                CreateCredential(),
                () =>
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                    return Task.CompletedTask;
                });
        }
        else
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken)
    {
        if (ShouldUseImpersonation())
        {
            return await _impersonationService.RunImpersonatedAsync(
                CreateCredential(),
                () =>
                {
                    return Task.FromResult(File.Exists(path));
                });
        }
        else
        {
            return File.Exists(path);
        }
    }

    public async Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken)
    {
        if (ShouldUseImpersonation())
        {
            return await _impersonationService.RunImpersonatedAsync(
                CreateCredential(),
                () =>
                {
                    if (!File.Exists(path))
                    {
                        throw new FileNotFoundException($"File not found: {path}");
                    }
                    return Task.FromResult<Stream>(File.OpenRead(path));
                });
        }
        else
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"File not found: {path}");
            }
            return File.OpenRead(path);
        }
    }

    public async Task WriteAsync(string path, Stream data, CancellationToken cancellationToken)
    {
        if (ShouldUseImpersonation())
        {
            await _impersonationService.RunImpersonatedAsync(
                CreateCredential(),
                async () =>
                {
                    using var fileStream = File.Create(path);
                    await data.CopyToAsync(fileStream, cancellationToken);
                });
        }
        else
        {
            using var fileStream = File.Create(path);
            await data.CopyToAsync(fileStream, cancellationToken);
        }
    }

    public bool IsFileLocked(string path)
    {
        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
    }

    public async IAsyncEnumerable<FileMetadata> ListFolderAsync(
        string path,
        [EnumeratorCancellation] CancellationToken cancellationToken,
        Func<FileMetadata, bool>? filter = null,
        bool recursive = false)
    {
        var files = ShouldUseImpersonation()
            ? await _impersonationService.RunImpersonatedAsync(
                CreateCredential(),
                () => EnumerateFilesAsync(path, cancellationToken, filter, recursive))
            : await EnumerateFilesAsync(path, cancellationToken, filter, recursive);

        foreach (var fileMetadata in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return fileMetadata;
            await Task.Yield();
        }
    }

    private async Task<List<FileMetadata>> EnumerateFilesAsync(
        string path,
        CancellationToken cancellationToken,
        Func<FileMetadata, bool>? filter,
        bool recursive)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Directory not found: {path}");
        }

        var result = new List<FileMetadata>();

        foreach (var file in Directory.EnumerateFiles(path))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileInfo = new FileInfo(file);
            var metadata = new FileMetadata
            {
                Path = file,
                Size = fileInfo.Length,
                LastModified = fileInfo.LastWriteTime,
                Extension = fileInfo.Extension
            };

            if (filter == null || filter(metadata))
            {
                result.Add(metadata);
            }
        }

        if (recursive)
        {
            foreach (var directory in Directory.EnumerateDirectories(path))
            {
                var subFiles = await EnumerateFilesAsync(directory, cancellationToken, filter, recursive);
                result.AddRange(subFiles);
            }
        }

        return result;
    }

    private NetworkCredential? CreateCredential()
    {
        return string.IsNullOrEmpty(_descriptor.Username)
            ? null
            : new NetworkCredential(_descriptor.Username, _descriptor.Password, _descriptor.Domain);
    }

    private bool ShouldUseImpersonation()
    {
        return !string.IsNullOrEmpty(_descriptor.Username);
    }
}
