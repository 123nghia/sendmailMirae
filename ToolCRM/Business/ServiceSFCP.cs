using Renci.SshNet;
using ToolCRM.Configuration;
using Microsoft.Extensions.Logging;

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
                    using (var sftp = new SftpClient(HOST, PORT, USERNAME, PASSWORD))
                    {
                        sftp.ConnectionInfo.Timeout = TimeSpan.FromMinutes(2);
                        sftp.Connect();
                        
                        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        {
                            sftp.UploadFile(fileStream, WorkingTimeFolder + fileName, true, progress =>
                            {
                                _logger.LogDebug($"Upload progress for {fileName}: {progress}%");
                            });
                        }
                        
                        sftp.Disconnect();
                    }
                    
                    // Verify upload by checking if file exists on remote server
                    if (await VerifyFileUploaded(WorkingTimeFolder + fileName))
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
                    using (var sftp = new SftpClient(HOST, PORT, USERNAME, PASSWORD))
                    {
                        sftp.ConnectionInfo.Timeout = TimeSpan.FromMinutes(2);
                        sftp.Connect();
                        
                        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        {
                            sftp.UploadFile(fileStream, CallReportFolder + fileName, true, progress =>
                            {
                                _logger.LogDebug($"Upload progress for {fileName}: {progress}%");
                            });
                        }
                        
                        sftp.Disconnect();
                    }
                    
                    // Verify upload by checking if file exists on remote server
                    if (await VerifyFileUploaded(CallReportFolder + fileName))
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
            try
            {
                using (var sftp = new SftpClient(HOST, PORT, USERNAME, PASSWORD))
                {
                    sftp.ConnectionInfo.Timeout = TimeSpan.FromSeconds(30);
                    sftp.Connect();
                    
                    var file = sftp.Get(remotePath);
                    var exists = file != null;
                    
                    sftp.Disconnect();
                    
                    _logger.LogDebug($"File verification for {remotePath}: {(exists ? "EXISTS" : "NOT FOUND")}");
                    
                    return exists;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Could not verify file existence for {remotePath}");
                return false;
            }
        }
        
        private async Task HandleFolderWorkingTime()
        {
            string localDirectory = Path.Combine(Directory.GetCurrentDirectory(), _appSettings.FilePaths.UploadWorkingTime);
            
            if (!Directory.Exists(localDirectory))
            {
                Directory.CreateDirectory(localDirectory);
                return;
            }
            
            var allFilesInFolder = Directory.GetFiles(localDirectory);
            _logger.LogInformation($"Found {allFilesInFolder.Length} files in WorkingTime folder");
            
            foreach (var filePath in allFilesInFolder)
            {
                await UploadFileWorkingTime(filePath);
            }
        }
        
        private async Task HandleFolderCDR()
        {
            string localDirectory = Path.Combine(Directory.GetCurrentDirectory(), _appSettings.FilePaths.UploadCallReport);
            
            if (!Directory.Exists(localDirectory))
            {
                Directory.CreateDirectory(localDirectory);
                return;
            }
            
            var allFilesInFolder = Directory.GetFiles(localDirectory);
            _logger.LogInformation($"Found {allFilesInFolder.Length} files in CallReport folder");
            
            foreach (var filePath in allFilesInFolder)
            {
                await UploadFileReportCDR(filePath);
            }
        }
        
        public async Task UploadFolderToSFCP()
        {
            try
            {
                _logger.LogInformation("Starting SFTP upload process...");
                
                await HandleFolderWorkingTime();
                await HandleFolderCDR();
                
                _logger.LogInformation("SFTP upload process completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during SFTP upload process");
                throw;
            }
        }
    }
}

