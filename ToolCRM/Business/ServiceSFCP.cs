using Renci.SshNet;
using ToolCRM.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace ToolCRM.Business
{
    public class ServiceSFCP
    {
        private readonly AppSettings _appSettings;
        private readonly ILogger<ServiceSFCP> _logger;
        private const int MAX_RETRY_ATTEMPTS = 3;
        private const int RETRY_DELAY_SECONDS = 5;
        
        public ServiceSFCP(AppSettings appSettings, ILogger<ServiceSFCP> logger)
        {
            _appSettings = appSettings;
            _logger = logger;
        }
        
        private string HOST => _appSettings.Sftp.Host;
        private string USERNAME => _appSettings.Sftp.Username;
        private string PASSWORD => _appSettings.Sftp.Password;
        private int PORT => _appSettings.Sftp.Port;
        private string WorkingTimeFolder => _appSettings.Sftp.WorkingTimeFolder;
        private string CallReportFolder => _appSettings.Sftp.CallReportFolder;
        
        private async Task<bool> UploadFileWorkingTime(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var fileSize = new FileInfo(filePath).Length;
            
            _logger.LogInformation($"Starting upload of WorkingTime file: {fileName}");
            
            for (int attempt = 1; attempt <= MAX_RETRY_ATTEMPTS; attempt++)
            {
                try
                {
                    _logger.LogInformation($"Attempting to upload {fileName} to {WorkingTimeFolder}");
                    
                    var remoteDirectory = NormalizeRemoteDirectory(WorkingTimeFolder);
                    var remotePath = BuildRemotePath(remoteDirectory, fileName);

                    using (var sftp = new SftpClient(HOST, PORT, USERNAME, PASSWORD))
                    {
                        sftp.ConnectionInfo.Timeout = TimeSpan.FromMinutes(2);
                        sftp.Connect();
                        _logger.LogInformation($"Connected to SFTP server: {HOST}:{PORT}");
                        
                        _logger.LogInformation($"Uploading to remote path: {remotePath}");
                        
                        EnsureRemoteDirectoryExists(sftp, remoteDirectory);
                        
                        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        {
                            sftp.UploadFile(fileStream, remotePath, true, progress =>
                            {
                                _logger.LogDebug($"Upload progress for {fileName}: {progress}%");
                            });
                        }
                        
                        // Verify file exists before disconnecting
                        var exists = sftp.Exists(remotePath);
                        _logger.LogInformation($"File verification after upload: {(exists ? "EXISTS" : "NOT FOUND")}");
                        
                        sftp.Disconnect();
                    }
                    
                    // Double check with separate verification
                    if (await VerifyFileUploaded(remotePath))
                    {
                        _logger.LogInformation($"Successfully uploaded WorkingTime file: {fileName} on attempt {attempt}");
                        
                        // Delete local file only after successful upload and verification
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                            _logger.LogInformation($"Deleted local file: {fileName}");
                        }
                        
                        return true;
                    }
                    else
                    {
                        throw new Exception($"File verification failed for {fileName}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to upload {fileName} on attempt {attempt}/{MAX_RETRY_ATTEMPTS}");
                    
                    if (attempt < MAX_RETRY_ATTEMPTS)
                    {
                        _logger.LogInformation($"Retrying upload in {RETRY_DELAY_SECONDS} seconds...");
                        await Task.Delay(TimeSpan.FromSeconds(RETRY_DELAY_SECONDS));
                    }
                    else
                    {
                        // Final attempt failed, log error
                        _logger.LogError($"Final upload attempt failed for {fileName}. Error: {ex.Message}");
                        
                        // Keep file locally for manual retry later
                        _logger.LogWarning($"Keeping local file {fileName} for manual retry");
                    }
                }
            }
            
            return false;
        }

        private async Task<bool> UploadFileReportCDR(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var fileSize = new FileInfo(filePath).Length;
            
            _logger.LogInformation($"Starting upload of CallReport file: {fileName}");
            
            for (int attempt = 1; attempt <= MAX_RETRY_ATTEMPTS; attempt++)
            {
                try
                {
                    _logger.LogInformation($"Attempting to upload {fileName} to {CallReportFolder}");
                    
                    var remoteDirectory = NormalizeRemoteDirectory(CallReportFolder);
                    var remotePath = BuildRemotePath(remoteDirectory, fileName);

                    using (var sftp = new SftpClient(HOST, PORT, USERNAME, PASSWORD))
                    {
                        sftp.ConnectionInfo.Timeout = TimeSpan.FromMinutes(2);
                        sftp.Connect();
                        _logger.LogInformation($"Connected to SFTP server: {HOST}:{PORT}");
                        
                        _logger.LogInformation($"Uploading to remote path: {remotePath}");
                        
                        EnsureRemoteDirectoryExists(sftp, remoteDirectory);
                        
                        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        {
                            sftp.UploadFile(fileStream, remotePath, true, progress =>
                            {
                                _logger.LogDebug($"Upload progress for {fileName}: {progress}%");
                            });
                        }
                        
                        // Verify file exists before disconnecting
                        var exists = sftp.Exists(remotePath);
                        _logger.LogInformation($"File verification after upload: {(exists ? "EXISTS" : "NOT FOUND")}");
                        
                        sftp.Disconnect();
                    }
                    
                    // Double check with separate verification
                    if (await VerifyFileUploaded(remotePath))
                    {
                        _logger.LogInformation($"Successfully uploaded CallReport file: {fileName} on attempt {attempt}");
                        
                        // Delete local file only after successful upload and verification
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                            _logger.LogInformation($"Deleted local file: {fileName}");
                        }
                        
                        return true;
                    }
                    else
                    {
                        throw new Exception($"File verification failed for {fileName}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to upload {fileName} on attempt {attempt}/{MAX_RETRY_ATTEMPTS}");
                    
                    if (attempt < MAX_RETRY_ATTEMPTS)
                    {
                        _logger.LogInformation($"Retrying upload in {RETRY_DELAY_SECONDS} seconds...");
                        await Task.Delay(TimeSpan.FromSeconds(RETRY_DELAY_SECONDS));
                    }
                    else
                    {
                        // Final attempt failed, log error
                        _logger.LogError($"Final upload attempt failed for {fileName}. Error: {ex.Message}");
                        
                        // Keep file locally for manual retry later
                        _logger.LogWarning($"Keeping local file {fileName} for manual retry");
                    }
                }
            }
            
            return false;
        }
        
        private async Task<bool> VerifyFileUploaded(string remotePath)
        {
            var normalizedPath = BuildNormalizedPath(remotePath);
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                _logger.LogWarning("Skipping verification because remote path is empty");
                return false;
            }

            try
            {
                using (var sftp = new SftpClient(HOST, PORT, USERNAME, PASSWORD))
                {
                    sftp.ConnectionInfo.Timeout = TimeSpan.FromSeconds(30);
                    sftp.Connect();
                    
                    var exists = sftp.Exists(normalizedPath);
                    
                    sftp.Disconnect();
                    
                    _logger.LogDebug($"File verification for {normalizedPath}: {(exists ? "EXISTS" : "NOT FOUND")}");
                    
                    return exists;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Could not verify file existence for {normalizedPath}");
                return false;
            }
        }

        private string NormalizeRemoteDirectory(string remoteDirectory)
        {
            if (string.IsNullOrWhiteSpace(remoteDirectory))
            {
                return "/";
            }

            var normalized = remoteDirectory.Replace("\\", "/").Trim();

            if (!normalized.StartsWith("/"))
            {
                normalized = "/" + normalized;
            }

            normalized = normalized.TrimEnd('/');

            if (string.IsNullOrWhiteSpace(normalized))
            {
                return "/";
            }

            return normalized;
        }

        private string BuildRemotePath(string remoteDirectory, string fileName)
        {
            var directory = NormalizeRemoteDirectory(remoteDirectory);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return directory;
            }

            if (directory == "/")
            {
                return "/" + fileName;
            }

            return directory + "/" + fileName;
        }

        private void EnsureRemoteDirectoryExists(SftpClient sftp, string remoteDirectory)
        {
            var directory = NormalizeRemoteDirectory(remoteDirectory);
            if (directory == "/")
            {
                return;
            }

            var parts = directory.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var currentPath = directory.StartsWith("/") ? "/" : string.Empty;

            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part))
                {
                    continue;
                }

                currentPath = currentPath == "/"
                    ? "/" + part
                    : currentPath.TrimEnd('/') + "/" + part;

                if (!sftp.Exists(currentPath))
                {
                    sftp.CreateDirectory(currentPath);
                    _logger.LogInformation($"Created remote directory: {currentPath}");
                }
            }
        }
        
        private string BuildNormalizedPath(string remotePath)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                return string.Empty;
            }

            var normalized = remotePath.Replace("\\", "/");
            if (!normalized.StartsWith("/"))
            {
                normalized = "/" + normalized;
            }

            return normalized.Replace("//", "/");
        }
        
        private async Task<List<string>> HandleFolderWorkingTime()
        {
            var failures = new List<string>();
            string localDirectory = Path.Combine(Directory.GetCurrentDirectory(), _appSettings.FilePaths.UploadWorkingTime);
            
            if (!Directory.Exists(localDirectory))
            {
                Directory.CreateDirectory(localDirectory);
                return failures;
            }
            
            var allFilesInFolder = Directory.GetFiles(localDirectory);
            _logger.LogInformation($"Found {allFilesInFolder.Length} files in WorkingTime folder");
            
            foreach (var filePath in allFilesInFolder)
            {
                if (!await UploadFileWorkingTime(filePath))
                {
                    var fileName = Path.GetFileName(filePath);
                    _logger.LogWarning($"Upload failed for WorkingTime file {fileName}");
                    failures.Add($"WorkingTime file '{fileName}'");
                }
            }

            return failures;
        }
        
        private async Task<List<string>> HandleFolderCDR()
        {
            var failures = new List<string>();
            string localDirectory = Path.Combine(Directory.GetCurrentDirectory(), _appSettings.FilePaths.UploadCallReport);
            
            if (!Directory.Exists(localDirectory))
            {
                Directory.CreateDirectory(localDirectory);
                return failures;
            }
            
            var allFilesInFolder = Directory.GetFiles(localDirectory);
            _logger.LogInformation($"Found {allFilesInFolder.Length} files in CallReport folder");
            
            foreach (var filePath in allFilesInFolder)
            {
                if (!await UploadFileReportCDR(filePath))
                {
                    var fileName = Path.GetFileName(filePath);
                    _logger.LogWarning($"Upload failed for CallReport file {fileName}");
                    failures.Add($"CallReport file '{fileName}'");
                }
            }

            return failures;
        }
        
        public async Task<List<string>> UploadFolderToSFCP()
        {
            var failures = new List<string>();

            try
            {
                _logger.LogInformation("Starting SFTP upload process...");

                var workingTimeFailures = await HandleFolderWorkingTime();
                failures.AddRange(workingTimeFailures);
                if (workingTimeFailures.Count == 0)
                {
                    _logger.LogInformation("HandleFolderWorkingTime completed");
                }
                else
                {
                    _logger.LogWarning($"HandleFolderWorkingTime completed with failures: {string.Join(", ", workingTimeFailures)}");
                }

                var callReportFailures = await HandleFolderCDR();
                failures.AddRange(callReportFailures);
                if (callReportFailures.Count == 0)
                {
                    _logger.LogInformation("HandleFolderCDR completed");
                }
                else
                {
                    _logger.LogWarning($"HandleFolderCDR completed with failures: {string.Join(", ", callReportFailures)}");
                }

                if (failures.Count == 0)
                {
                    _logger.LogInformation("SFTP upload process completed");
                }
                else
                {
                    _logger.LogWarning("SFTP upload process completed with failures");
                }

                return failures;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during SFTP upload process");
                throw;
            }
        }
    }
}

