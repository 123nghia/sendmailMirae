namespace ToolCRM.Configuration
{
    public class AppSettings
    {
        public FilePathsSettings FilePaths { get; set; } = new();
        public SftpSettings Sftp { get; set; } = new();
        public EmailSettings Email { get; set; } = new();
    }

    public class FilePathsSettings
    {
        public string SourceFile { get; set; } = string.Empty;
        public string UploadWorkingTime { get; set; } = string.Empty;
        public string UploadCallReport { get; set; } = string.Empty;
        public string Logs { get; set; } = string.Empty;
    }

    public class SftpSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string WorkingTimeFolder { get; set; } = string.Empty;
        public string CallReportFolder { get; set; } = string.Empty;
        public string PaymentFolder { get; set; } = string.Empty;
    }

    public class EmailSettings
    {
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 465;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Recipient { get; set; } = string.Empty;
    }
}
