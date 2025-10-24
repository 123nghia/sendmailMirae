using Renci.SshNet;
using ToolCRM.Configuration;

namespace ToolCRM.Libraries.SendEmailLibrary
{
    public class ServiceSFCP
    {
        private readonly AppSettings _appSettings;

        public ServiceSFCP(AppSettings appSettings)
        {
            _appSettings = appSettings;
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
            var remoteUPloadWorkingTime = _appSettings.RemotePaths.WorkingTime;
            var remoteUPloadCallReport = _appSettings.RemotePaths.CallReport;
            try
            {
                using (var sftp = new SftpClient(_appSettings.SFTP.Host, _appSettings.SFTP.Port, _appSettings.SFTP.Username, _appSettings.SFTP.Password))
                {
                    sftp.ConnectionInfo.Timeout = TimeSpan.FromSeconds(10);
                    
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    await Task.Run(() => sftp.Connect(), cts.Token);
                    
                    if (File.Exists(fullPathWorkingTimeReport))
                    {
                        FileInfo fileWorkingTime = new FileInfo(fullPathWorkingTimeReport);
                        using (var fileStream = new FileStream(fileWorkingTime.FullName, FileMode.Open, FileAccess.Read))
                        {
                            sftp.UploadFile(fileStream, remoteUPloadWorkingTime + fileWorkingTime.Name, null);
                        }
                    }
                    if (File.Exists(fullPathCallReport))
                    {
                        FileInfo fileCallreport = new FileInfo(fullPathCallReport);
                        using (var fileStream2 = new FileStream(fileCallreport.FullName, FileMode.Open, FileAccess.Read))
                        {
                            sftp.UploadFile(fileStream2, remoteUPloadCallReport + fileCallreport.Name, null);
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

            if (File.Exists(fullPathWorkingTimeReport))
            {
                File.Delete(fullPathWorkingTimeReport);
            }

            if (File.Exists(fullPathCallReport))
            {
                File.Delete(fullPathCallReport);
            }
        }
    }
}