namespace ToolCRM.Models
{
    public class SftpFileModel
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public string Extension { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        public string SizeFormatted { get; set; } = string.Empty;
    }

    public class SftpDirectoryModel
    {
        public string CurrentPath { get; set; } = string.Empty;
        public string ParentPath { get; set; } = string.Empty;
        public List<SftpFileModel> Files { get; set; } = new();
        public List<SftpFileModel> Directories { get; set; } = new();
    }
}
