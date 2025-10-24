using ToolCRM.Configuration;
using ToolCRM.Models;
using System.Text.Json;

namespace ToolCRM.Services
{
    public class LoggingService
    {
        private readonly AppSettings _appSettings;
        private readonly string _logDirectory;

        public LoggingService(AppSettings appSettings)
        {
            _appSettings = appSettings;
            _logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            
            // Tạo thư mục Logs nếu chưa tồn tại
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public async Task LogEmailSent(string emailType, string recipient, string subject, bool success, string? errorMessage = null)
        {
            var now = DateTime.Now;
            var logEntry = new
            {
                Timestamp = now.ToString("yyyy-MM-dd HH:mm:ss"),
                Date = now.ToString("yyyy-MM-dd"),
                Time = now.ToString("HH:mm:ss"),
                DayOfWeek = now.ToString("dddd"),
                Action = "EMAIL_SENT",
                Type = emailType,
                Recipient = recipient,
                Subject = subject,
                Success = success,
                ErrorMessage = errorMessage
            };

            await WriteLogAsync("email_history.txt", logEntry);
        }

        public async Task LogFileUpload(string fileName, string fileType, string localPath, string destination, bool success, string? errorMessage = null)
        {
            var now = DateTime.Now;
            var fileInfo = new FileInfo(localPath);
            var logEntry = new
            {
                Timestamp = now.ToString("yyyy-MM-dd HH:mm:ss"),
                Date = now.ToString("yyyy-MM-dd"),
                Time = now.ToString("HH:mm:ss"),
                DayOfWeek = now.ToString("dddd"),
                Action = "FILE_UPLOAD",
                FileName = fileName,
                FileType = fileType,
                LocalPath = localPath,
                LocalFileName = fileInfo.Name,
                FileSize = fileInfo.Exists ? fileInfo.Length : 0,
                Destination = destination,
                Success = success,
                ErrorMessage = errorMessage,
                CanDownload = fileInfo.Exists
            };

            await WriteLogAsync("upload_history.txt", logEntry);
        }

        public async Task LogFileDownload(string fileName, string source, string localPath, bool success, string? errorMessage = null)
        {
            var now = DateTime.Now;
            var fileInfo = new FileInfo(localPath);
            var logEntry = new
            {
                Timestamp = now.ToString("yyyy-MM-dd HH:mm:ss"),
                Date = now.ToString("yyyy-MM-dd"),
                Time = now.ToString("HH:mm:ss"),
                DayOfWeek = now.ToString("dddd"),
                Action = "FILE_DOWNLOAD",
                FileName = fileName,
                Source = source,
                LocalPath = localPath,
                LocalFileName = fileInfo.Name,
                FileSize = fileInfo.Exists ? fileInfo.Length : 0,
                Success = success,
                ErrorMessage = errorMessage,
                CanDownload = fileInfo.Exists
            };

            await WriteLogAsync("download_history.txt", logEntry);
        }

        public async Task LogFileProcessing(string fileName, string processType, bool success, string? errorMessage = null)
        {
            var now = DateTime.Now;
            var logEntry = new
            {
                Timestamp = now.ToString("yyyy-MM-dd HH:mm:ss"),
                Date = now.ToString("yyyy-MM-dd"),
                Time = now.ToString("HH:mm:ss"),
                DayOfWeek = now.ToString("dddd"),
                Action = "FILE_PROCESSING",
                FileName = fileName,
                ProcessType = processType,
                Success = success,
                ErrorMessage = errorMessage
            };

            await WriteLogAsync("processing_history.txt", logEntry);
        }

        public async Task LogError(string errorType, string errorMessage, string? stackTrace = null, string? additionalInfo = null)
        {
            var now = DateTime.Now;
            var logEntry = new
            {
                Timestamp = now.ToString("yyyy-MM-dd HH:mm:ss"),
                Date = now.ToString("yyyy-MM-dd"),
                Time = now.ToString("HH:mm:ss"),
                DayOfWeek = now.ToString("dddd"),
                Action = "ERROR_LOG",
                ErrorType = errorType,
                ErrorMessage = errorMessage,
                StackTrace = stackTrace,
                AdditionalInfo = additionalInfo,
                UserAgent = "ToolCRM",
                IPAddress = "127.0.0.1"
            };

            await WriteLogAsync("error_log.txt", logEntry);
        }

        public async Task LogOperation(string operationType, string operationName, bool success, string? details = null, string? errorMessage = null)
        {
            var now = DateTime.Now;
            var logEntry = new
            {
                Timestamp = now.ToString("yyyy-MM-dd HH:mm:ss"),
                Date = now.ToString("yyyy-MM-dd"),
                Time = now.ToString("HH:mm:ss"),
                DayOfWeek = now.ToString("dddd"),
                Action = "OPERATION_LOG",
                OperationType = operationType,
                OperationName = operationName,
                Success = success,
                Details = details,
                ErrorMessage = errorMessage
            };

            await WriteLogAsync("operation_log.txt", logEntry);
        }

        private async Task WriteLogAsync(string fileName, object logEntry)
        {
            try
            {
                var logPath = Path.Combine(_logDirectory, fileName);
                var logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {System.Text.Json.JsonSerializer.Serialize(logEntry)}{Environment.NewLine}";
                
                await File.AppendAllTextAsync(logPath, logLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing log: {ex.Message}");
            }
        }

        public async Task<List<LogEntry>> GetEmailHistory(int maxLines = 100)
        {
            return await ReadLogEntries("email_history.txt", maxLines);
        }

        public async Task<List<LogEntry>> GetUploadHistory(int maxLines = 100)
        {
            return await ReadLogEntries("upload_history.txt", maxLines);
        }

        public async Task<List<LogEntry>> GetDownloadHistory(int maxLines = 100)
        {
            return await ReadLogEntries("download_history.txt", maxLines);
        }

        public async Task<List<LogEntry>> GetProcessingHistory(int maxLines = 100)
        {
            return await ReadLogEntries("processing_history.txt", maxLines);
        }

        private async Task<List<LogEntry>> ReadLogEntries(string fileName, int maxLines)
        {
            var entries = new List<LogEntry>();
            try
            {
                var logPath = Path.Combine(_logDirectory, fileName);
                if (!File.Exists(logPath))
                    return entries;

                var content = await File.ReadAllTextAsync(logPath);
                var jsonBlocks = content.Split(new[] { Environment.NewLine + Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var block in jsonBlocks.TakeLast(maxLines))
                {
                    try
                    {
                        var entry = JsonSerializer.Deserialize<LogEntry>(block.Trim());
                        if (entry != null)
                            entries.Add(entry);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing log entry: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading log file {fileName}: {ex.Message}");
            }

            return entries.OrderByDescending(e => e.Timestamp).ToList();
        }

        public async Task ClearOldLogs(int daysToKeep = 30)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                var logFiles = Directory.GetFiles(_logDirectory, "*.txt");

                foreach (var file in logFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing old logs: {ex.Message}");
            }
        }
    }
}
