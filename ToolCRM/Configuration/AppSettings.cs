namespace ToolCRM.Configuration
{
    public class AppSettings
    {
        public SFTPSettings SFTP { get; set; } = new();
        public EmailSettings Email { get; set; } = new();
        public PathsSettings Paths { get; set; } = new();
        public RemotePathsSettings RemotePaths { get; set; } = new();
    }

    public class SFTPSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class EmailSettings
    {
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string ToEmail { get; set; } = string.Empty;
        public List<string> CCEmails { get; set; } = new();
    }

    public class PathsSettings
    {
        public string LocalFile { get; set; } = string.Empty;
        public string LocalSendFile { get; set; } = string.Empty;
        public string LocalDirectory { get; set; } = string.Empty;
        public string ToolCRMSourceFile { get; set; } = string.Empty;
        public string ToolCRMUploaWorkingTime { get; set; } = string.Empty;
        public string ToolCRMUploaCallReport { get; set; } = string.Empty;
        public string DataTcFile { get; set; } = string.Empty;
        public string TemplateFile { get; set; } = string.Empty;
    }

    public class RemotePathsSettings
    {
        public string Payment { get; set; } = string.Empty;
        public string WorkingTime { get; set; } = string.Empty;
        public string CallReport { get; set; } = string.Empty;
    }

}
