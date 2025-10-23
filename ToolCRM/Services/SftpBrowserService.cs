using Renci.SshNet;
using ToolCRM.Configuration;
using ToolCRM.Models;
using Microsoft.Extensions.Options;

namespace ToolCRM.Services
{
    public class SftpBrowserService
    {
        private readonly AppSettings _appSettings;

        public SftpBrowserService(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        public async Task<SftpDirectoryModel> BrowseDirectoryAsync(string path = "/")
        {
            var result = new SftpDirectoryModel
            {
                CurrentPath = path,
                ParentPath = GetParentPath(path)
            };

            try
            {
                using var sftp = new SftpClient(_appSettings.SFTP.Host, _appSettings.SFTP.Port, _appSettings.SFTP.Username, _appSettings.SFTP.Password);
                sftp.ConnectionInfo.Timeout = TimeSpan.FromMinutes(2);
                sftp.Connect();

                var files = sftp.ListDirectory(path);
                var fileList = new List<SftpFileModel>();

                foreach (var file in files)
                {
                    if (file.Name == "." || file.Name == "..") continue;

                    var sftpFile = new SftpFileModel
                    {
                        Name = file.Name,
                        FullPath = file.FullName,
                        IsDirectory = file.IsDirectory,
                        Size = file.Length,
                        LastModified = file.LastWriteTime,
                        Extension = Path.GetExtension(file.Name).ToLower(),
                        IconClass = GetIconClass(file.Name, file.IsDirectory),
                        SizeFormatted = FormatFileSize(file.Length)
                    };

                    fileList.Add(sftpFile);
                }

                // Sắp xếp theo thời gian tạo (mới nhất trước)
                fileList = fileList.OrderByDescending(f => f.LastModified).ToList();

                // Tách thư mục và file
                result.Directories = fileList.Where(f => f.IsDirectory).ToList();
                result.Files = fileList.Where(f => !f.IsDirectory).ToList();

                sftp.Disconnect();
            }
            catch (Exception ex)
            {
                // Log error if needed
                Console.WriteLine($"Error browsing SFTP directory: {ex.Message}");
            }

            return result;
        }

        public async Task<Stream> DownloadFileAsync(string filePath)
        {
            var memoryStream = new MemoryStream();
            
            try
            {
                using var sftp = new SftpClient(_appSettings.SFTP.Host, _appSettings.SFTP.Port, _appSettings.SFTP.Username, _appSettings.SFTP.Password);
                sftp.ConnectionInfo.Timeout = TimeSpan.FromMinutes(5);
                sftp.Connect();

                sftp.DownloadFile(filePath, memoryStream);
                memoryStream.Position = 0;

                sftp.Disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading file: {ex.Message}");
                throw;
            }

            return memoryStream;
        }

        private string GetParentPath(string currentPath)
        {
            if (currentPath == "/" || string.IsNullOrEmpty(currentPath))
                return "/";

            var parent = Path.GetDirectoryName(currentPath.TrimEnd('/'));
            return string.IsNullOrEmpty(parent) ? "/" : parent;
        }

        private string GetIconClass(string fileName, bool isDirectory)
        {
            if (isDirectory)
                return "fas fa-folder text-warning";

            var extension = Path.GetExtension(fileName).ToLower();
            return extension switch
            {
                ".pdf" => "fas fa-file-pdf text-danger",
                ".doc" or ".docx" => "fas fa-file-word text-primary",
                ".xls" or ".xlsx" => "fas fa-file-excel text-success",
                ".ppt" or ".pptx" => "fas fa-file-powerpoint text-warning",
                ".txt" => "fas fa-file-alt text-secondary",
                ".jpg" or ".jpeg" or ".png" or ".gif" => "fas fa-file-image text-info",
                ".zip" or ".rar" or ".7z" => "fas fa-file-archive text-dark",
                ".mp4" or ".avi" or ".mkv" => "fas fa-file-video text-danger",
                ".mp3" or ".wav" => "fas fa-file-audio text-success",
                _ => "fas fa-file text-secondary"
            };
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 B";

            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }
}
