﻿namespace FileMonitor.Core.Models;

public class FileMetadata
{
    public string Path { get; set; }
    public long Size { get; set; }
    public DateTime LastModified { get; set; }

    public string Extension { get; set; }
}
