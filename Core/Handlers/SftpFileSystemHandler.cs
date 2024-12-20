﻿namespace FileMonitor.Core.Handlers;

public class SftpFileSystemHandler : IFileSystemHandler, IAsyncDisposable
{
    private SftpClient _client;

    public SftpFileSystemHandler(SftpFolderDescriptor descriptor)
    {
        if (descriptor == null)
            throw new ArgumentNullException(nameof(descriptor));

        _client = new SftpClient(descriptor.Host, descriptor.Port, descriptor.Username, descriptor.Password);
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        if (!_client.IsConnected)
        {
            await _client.ConnectAsync(cancellationToken); ; // S'assure que Connect() est exécuté sans bloquer.
        }  
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync(cancellationToken);

        // Méthode async native :
        await _client.DeleteFileAsync(path, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync(cancellationToken);
        return await _client.ExistsAsync(path, cancellationToken);
    }

    public async Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync(cancellationToken);

        if (!await _client.ExistsAsync(path, cancellationToken))
        {
            throw new FileNotFoundException($"File not found: {path}");
        }

        // Utilisation de OpenAsync avec les bons paramètres
        var sftpStream = await _client.OpenAsync(path, FileMode.Open, FileAccess.Read, cancellationToken);
        return sftpStream; // SftpFileStream hérite de Stream, donc c'est compatible
    }

    public async Task WriteAsync(string path, Stream data, CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync(cancellationToken);

        if (data.CanSeek)
        {
            data.Position = 0;
        }

        // On ouvre le fichier en écriture via la méthode synchrone.
        // Puisque c'est une opération rapide, on peut l'accepter telle quelle ou la mettre dans un Task.Run.
        using var stream = _client.Open(path, FileMode.Create, FileAccess.Write);

        // Maintenant, on peut utiliser CopyToAsync pour copier de façon asynchrone.
        // CopyToAsync utilisera ReadAsync/WriteAsync sur les flux, 
        // ce qui ne bloquera pas le thread appelant.
        await data.CopyToAsync(stream, 81920, cancellationToken);
    }

    public bool IsFileLocked(string path)
    {
        // Sur SFTP, pas de concept de verrouillage fichier
        return false;
    }

    public async IAsyncEnumerable<FileMetadata> ListFolderAsync(
        string path,
        [EnumeratorCancellation] CancellationToken cancellationToken,
        Func<FileMetadata, bool>? filter = null,
        bool recursive = false)
    {
        await EnsureConnectedAsync(cancellationToken);

        // Méthode async native retournant IAsyncEnumerable<SftpFile> :
        await foreach (var file in _client.ListDirectoryAsync(path, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (file.Name == "." || file.Name == "..")
            {
                continue;
            }

            if (file.IsDirectory && recursive)
            {
                await foreach (var subFile in ListFolderAsync(file.FullName, cancellationToken, filter, recursive))
                {
                    yield return subFile;
                }
            }
            else if (!file.IsDirectory)
            {
                var metadata = new FileMetadata
                {
                    Path = file.FullName,
                    Size = file.Length,
                    LastModified = file.LastWriteTime,
                    Extension= Path.GetExtension(file.FullName)
                };

                if (filter == null || filter(metadata))
                {
                    yield return metadata;
                }

                await Task.Yield();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_client != null)
        {
            if (_client.IsConnected)
            {
                // S'il existe DisconnectAsync, utilisez-le, sinon synchro:
                //await _client.DisconnectAsync();

                await Task.Run(() => _client.Disconnect());
            }
            _client.Dispose();
            _client = null;
        }
    }
}
