using Renci.SshNet;
using ToolCRM.Configuration;
using ToolCRM.Services;

namespace ToolCRM.Libraries.SendEmailLibrary
{
    public class ServiceSFCP
    {
        private readonly AppSettings _appSettings;
        private readonly LoggingService _loggingService;

        public ServiceSFCP(AppSettings appSettings)
        {
            _appSettings = appSettings;
            _loggingService = new LoggingService(appSettings);
        }

        private string GetFriendlyErrorMessage(Exception ex)
        {
            return ex switch
            {
                System.Net.Sockets.SocketException => "Lỗi kết nối mạng: Không thể kết nối đến server SFTP",
                Renci.SshNet.Common.SshConnectionException => "Lỗi xác thực: Sai thông tin đăng nhập SFTP",
                Renci.SshNet.Common.SshOperationTimeoutException => "Timeout: Server SFTP không phản hồi",
                _ => ex.Message
            };
        }

        public async Task<(bool hasNewFile, string fileName)> DowloadFilePayment()
        {
            string localDirectory = _appSettings.Paths.LocalFile;
            string remoteDirectory = _appSettings.RemotePaths.Payment;
            var filePayment = "payment_" + DateTime.Now.ToString("yyyyMMdd") + ".xlsx";
            var fullPathfilePayment = Path.Combine(localDirectory, filePayment);
            
            // Check if file already exists locally
            if (File.Exists(fullPathfilePayment))
            {
                return (false, filePayment);
            }
            
            var fullPathfilePaymentSended = Path.Combine(_appSettings.Paths.LocalSendFile, filePayment);
            if (File.Exists(fullPathfilePaymentSended))
            {
                return (false, filePayment);
            }
            
            bool hasNewFile = false;
            string downloadedFileName = "";
            
            try
            {
                using (var sftp = new SftpClient(_appSettings.SFTP.Host, _appSettings.SFTP.Port, _appSettings.SFTP.Username, _appSettings.SFTP.Password))
                {
                    sftp.ConnectionInfo.Timeout = TimeSpan.FromSeconds(10);
                    
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    await Task.Run(() => sftp.Connect(), cts.Token);
                    
                    var files = sftp.ListDirectory(remoteDirectory);
                    
                    foreach (var item in files)
                    {
                        string remoteFileName = item.Name;
                        if (item.LastWriteTime.Date == DateTime.Today && item.IsRegularFile == true)
                        {
                            try
                            {
                                using (Stream file1 = File.Create(localDirectory + remoteFileName))
                                {
                                    sftp.DownloadFile(remoteDirectory + "/" + remoteFileName, file1);
                                    hasNewFile = true;
                                    downloadedFileName = remoteFileName;
                                    Console.WriteLine($"Downloaded new payment file: {remoteFileName}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error downloading file {remoteFileName}: {ex.Message}");
                                throw;
                            }
                        }
                    }
                    sftp.Disconnect();
                }
            }
            catch (OperationCanceledException)
            {
                throw new Exception("Timeout: Không thể kết nối SFTP trong 10 giây");
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi SFTP: {GetFriendlyErrorMessage(ex)}");
            }
            
            return (hasNewFile, downloadedFileName);
        }

        public Task UploadFileToRemoteFolder()
        {
            var dateHandle = DateTime.Now.AddDays(0);
            if (dateHandle.DayOfWeek == DayOfWeek.Monday)
            {
                dateHandle = dateHandle.AddDays(-2);
            }
            else
                dateHandle = dateHandle.AddDays(-1);
            dateHandle = dateHandle.Date;
            var sufixFile = dateHandle.ToString("yyyyMMdd") + ".xlsx";
            var localFileWorkingTime = _appSettings.Paths.ToolCRMUploaWorkingTime;
            var localFileCallReport = _appSettings.Paths.ToolCRMUploaCallReport;
            var remoteUPloadWorkingTime = _appSettings.RemotePaths.WorkingTime;
            var remoteUPloadCallReport = _appSettings.RemotePaths.CallReport;
            var fullPathWorkingTimeReport = Path.Combine(localFileWorkingTime, "working_time_" + sufixFile);
            var fullPathCallReport = Path.Combine(localFileCallReport, "call_report_" + sufixFile);
            var streams = new List<Stream>();

            using (var sftp = new SftpClient(_appSettings.SFTP.Host, _appSettings.SFTP.Port, _appSettings.SFTP.Username, _appSettings.SFTP.Password))
            {
                sftp.ConnectionInfo.Timeout = TimeSpan.FromMinutes(2);
                sftp.Connect();
                if (File.Exists(fullPathWorkingTimeReport))
                {
                    FileInfo fileWorkingTime = new FileInfo(fullPathWorkingTimeReport);
                    var fileStream = new FileStream(fileWorkingTime.FullName, FileMode.Open);
                    if (fileStream != null)
                    {
                        sftp.UploadFile(fileStream, remoteUPloadWorkingTime + fileWorkingTime.Name, null);
                        streams.Add(fileStream);
                    }
                }
                if (File.Exists(fullPathCallReport))
                {
                    FileInfo fileCallreport = new FileInfo(fullPathCallReport);
                    var fileStream2 = new FileStream(fileCallreport.FullName, FileMode.Open);
                    if (fileStream2 != null)
                    {
                        sftp.UploadFile(fileStream2, remoteUPloadCallReport + fileCallreport.Name, null);
                        streams.Add(fileStream2);
                    }
                }

                sftp.Disconnect();
                sftp.Dispose();
            }
            foreach (var item1 in streams)
            {
                item1.Dispose();
            }

            if (File.Exists(fullPathWorkingTimeReport))
            {
                File.Delete(fullPathWorkingTimeReport);
            }

            if (File.Exists(fullPathCallReport))
            {
                File.Delete(fullPathCallReport);
            }
            return Task.CompletedTask;
        }

        public async Task UploadFolderToSFCP()
        {
            var dateGet = DateTime.Now.ToString("yyyyMMdd");
            var fullPathWorkingTimeReport = Path.Combine(_appSettings.Paths.ToolCRMUploaWorkingTime, "working_time_" + dateGet + ".xlsx");
            var fullPathCallReport = Path.Combine(_appSettings.Paths.ToolCRMUploaCallReport, "call_report_" + dateGet + ".xlsx");
            var remoteWorkingTimeDir = _appSettings.RemotePaths.WorkingTime;
            var remoteCallReportDir = _appSettings.RemotePaths.CallReport;
            
            try
            {
                using (var sftp = new SftpClient(_appSettings.SFTP.Host, _appSettings.SFTP.Port, _appSettings.SFTP.Username, _appSettings.SFTP.Password))
                {
                    sftp.ConnectionInfo.Timeout = TimeSpan.FromSeconds(30);
                    
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    await Task.Run(() => sftp.Connect(), cts.Token);
                    
                    // Upload Working Time file
                    if (File.Exists(fullPathWorkingTimeReport))
                    {
                        await UploadFileToRemote(sftp, fullPathWorkingTimeReport, remoteWorkingTimeDir, "WorkingTime");
                    }
                    
                    // Upload Call Report file
                    if (File.Exists(fullPathCallReport))
                    {
                        await UploadFileToRemote(sftp, fullPathCallReport, remoteCallReportDir, "CallReport");
                    }

                    sftp.Disconnect();
                }
            }
            catch (OperationCanceledException)
            {
                throw new Exception("Timeout: Không thể kết nối SFTP trong 30 giây");
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi SFTP: {GetFriendlyErrorMessage(ex)}");
            }

            // Xóa file local sau khi upload thành công
            if (File.Exists(fullPathWorkingTimeReport))
            {
                File.Delete(fullPathWorkingTimeReport);
            }

            if (File.Exists(fullPathCallReport))
            {
                File.Delete(fullPathCallReport);
            }
        }

        private async Task UploadFileToRemote(SftpClient sftp, string localFilePath, string remoteDir, string fileType)
        {
            var fileName = Path.GetFileName(localFilePath);
            
            // Đảm bảo remoteDir có dấu "/" ở cuối
            if (!remoteDir.EndsWith("/"))
            {
                remoteDir += "/";
            }
            
            var remotePath = remoteDir + fileName;
            
            try
            {
                // Kiểm tra file có tồn tại trên remote không
                bool fileExists = false;
                try
                {
                    var remoteFiles = sftp.ListDirectory(remoteDir);
                    fileExists = remoteFiles.Any(f => f.Name == fileName && f.IsRegularFile);
                }
                catch
                {
                    // Thư mục chưa tồn tại hoặc lỗi đọc
                    fileExists = false;
                }

                // Upload file
                using (var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read))
                {
                    sftp.UploadFile(fileStream, remotePath, null);
                }

                // Ghi log
                string action = fileExists ? "UPDATED" : "UPLOADED";
                Console.WriteLine($"✅ {action} {fileType}: {fileName} → {remotePath}");
                
                await _loggingService.LogFileUpload(
                    fileName,
                    fileType,
                    localFilePath,
                    remotePath,
                    true,
                    action
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Upload failed {fileType}: {fileName} - {ex.Message}");
                await _loggingService.LogFileUpload(
                    fileName,
                    fileType,
                    localFilePath,
                    remotePath,
                    false,
                    ex.Message
                );
                throw;
            }
        }
    }
}