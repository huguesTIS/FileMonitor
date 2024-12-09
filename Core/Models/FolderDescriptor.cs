namespace FileMonitor.Core.Models;

public abstract class FolderDescriptor
{
    public string Path { get; set; } = string.Empty;
    public string Descrition { get; set; } = string.Empty;
}

public class SftpFolderDescriptor : FolderDescriptor
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 22;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LocalFolderDescriptor : FolderDescriptor
{

}