namespace ToolCRM.Models
{
    public class LogViewModel
    {
        public List<LogEntry> EmailHistory { get; set; } = new();
        public List<LogEntry> UploadHistory { get; set; } = new();
        public List<LogEntry> DownloadHistory { get; set; } = new();
        public List<LogEntry> ProcessingHistory { get; set; } = new();
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Recipient { get; set; }
        public string? Subject { get; set; }
        public string? FileName { get; set; }
        public string? FileType { get; set; }
        public long FileSize { get; set; }
        public string? Destination { get; set; }
        public string? Source { get; set; }
        public string? ProcessType { get; set; }
        public bool CanDownload { get; set; }

        public string GetDisplayName()
        {
            return FileName ?? "Unknown";
        }

        public string GetFileSizeDisplay()
        {
            if (FileSize < 1024) return $"{FileSize} B";
            if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:F2} KB";
            return $"{FileSize / (1024.0 * 1024.0):F2} MB";
        }
    }
}
