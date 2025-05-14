using System;

namespace FileClient
{
    public class FileItem
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }

        public string SizeDisplay => $"{Size / 1024.0:F1} KB";
        public string LastModifiedDisplay => LastModified.ToString("dd.MM.yyyy HH:mm");
    }
}
