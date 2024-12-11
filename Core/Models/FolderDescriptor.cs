namespace FileMonitor.Core.Models;

public abstract class FolderDescriptor
{
    public string Path { get; set; } = string.Empty;
    public string Descrition { get; set; } = string.Empty;

    // Retry strategy override
    public int? MaxRetries { get; set; } = null;
    public double? BackoffFactor { get; set; } = null;
}

public class SftpFolderDescriptor(string host, string username, string password, string rootPath, int? port = null) : FolderDescriptor
{
    public string Host { get; set; } = host ?? throw new ArgumentNullException(nameof(host));
    public int Port { get; set; } = port ?? 22;
    public string Username { get; set; } = username ?? throw new ArgumentNullException(nameof(username));
    public string Password { get; set; } = password ?? throw new ArgumentNullException(nameof(password));
    public string RootPath { get; set; } = rootPath ?? "/";

    // Retry strategy override
    public int? MaxRetries { get; set; } = 7;
    public double? BackoffFactor { get; set; } = 3000;
}

public class SmbFolderDescriptor(string uncPath, string username, string password, string domain = "") : FolderDescriptor
{
    public string UncPath { get; set; } = uncPath ?? throw new ArgumentNullException(nameof(uncPath));
    public string Username { get; set; } = username ?? throw new ArgumentNullException(nameof(username));
    public string Password { get; set; } = password ?? throw new ArgumentNullException(nameof(password));
    public string Domain { get; set; } = domain;

    // Retry strategy override
    public int? MaxRetries { get; set; } = 5;
    public double? BackoffFactor { get; set; } = 2000;
}

public class LocalFolderDescriptor : FolderDescriptor
{
    public LocalFolderDescriptor(string path)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
    }
}