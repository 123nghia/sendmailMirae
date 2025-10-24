using System.Text.Json.Serialization;

namespace ToolCRM.Models
{
    public class LogEntry
    {
        [JsonPropertyName("Timestamp")]
        public string Timestamp { get; set; } = string.Empty;

        [JsonPropertyName("Date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("Time")]
        public string Time { get; set; } = string.Empty;

        [JsonPropertyName("DayOfWeek")]
        public string DayOfWeek { get; set; } = string.Empty;

        [JsonPropertyName("Action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("FileName")]
        public string? FileName { get; set; }

        [JsonPropertyName("FileType")]
        public string? FileType { get; set; }

        [JsonPropertyName("LocalPath")]
        public string? LocalPath { get; set; }

        [JsonPropertyName("LocalFileName")]
        public string? LocalFileName { get; set; }

        [JsonPropertyName("FileSize")]
        public long? FileSize { get; set; }

        [JsonPropertyName("Source")]
        public string? Source { get; set; }

        [JsonPropertyName("Destination")]
        public string? Destination { get; set; }

        [JsonPropertyName("Type")]
        public string? Type { get; set; }

        [JsonPropertyName("Recipient")]
        public string? Recipient { get; set; }

        [JsonPropertyName("Subject")]
        public string? Subject { get; set; }

        [JsonPropertyName("ProcessType")]
        public string? ProcessType { get; set; }

        [JsonPropertyName("Success")]
        public bool Success { get; set; }

        [JsonPropertyName("ErrorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("CanDownload")]
        public bool CanDownload { get; set; }

        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(LocalFileName))
                return LocalFileName;
            if (!string.IsNullOrEmpty(FileName))
                return FileName;
            return "Unknown File";
        }

        public string GetFileSizeDisplay()
        {
            if (FileSize == null || FileSize == 0)
                return "0 B";

            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = FileSize.Value;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}
