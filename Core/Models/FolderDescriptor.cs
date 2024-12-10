namespace FileMonitor.Core.Models;

public abstract class FolderDescriptor
{
    public string Path { get; set; } = string.Empty;
    public string Descrition { get; set; } = string.Empty;
}

public class SftpFolderDescriptor
{
    public string Host { get; set; }
    public int Port { get; set; } = 22; // Valeur par défaut pour SFTP
    public string Username { get; set; }
    public string Password { get; set; }
    public string RootPath { get; set; } = "/"; // Optionnel : chemin racine par défaut

    public SftpFolderDescriptor(string host, string username, string password, string rootPath, int? port = null)
    {
        Host = host ?? throw new ArgumentNullException(nameof(host));
        Username = username ?? throw new ArgumentNullException(nameof(username));
        Password = password ?? throw new ArgumentNullException(nameof(password));
        RootPath = rootPath ?? "/";
        Port = port ?? 22;
    }
}

public class SmbFolderDescriptor
{
    public string UncPath { get; set; } // Le chemin réseau (UNC)
    public string Username { get; set; }
    public string Password { get; set; }
    public string Domain { get; set; } // Facultatif pour certains environnements

    public SmbFolderDescriptor(string uncPath, string username, string password, string domain = "")
    {
        UncPath = uncPath ?? throw new ArgumentNullException(nameof(uncPath));
        Username = username ?? throw new ArgumentNullException(nameof(username));
        Password = password ?? throw new ArgumentNullException(nameof(password));
        Domain = domain;
    }
}

public class LocalFolderDescriptor : FolderDescriptor
{
    public LocalFolderDescriptor(string path)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
    }
}