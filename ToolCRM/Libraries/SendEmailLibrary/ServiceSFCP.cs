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

        public Task DowloadFilePayment()
        {
            string localDirectory = _appSettings.Paths.LocalFile;
            string remoteDirectory = _appSettings.RemotePaths.Payment;
            var filePayment = "payment_" + DateTime.Now.ToString("yyyyMMdd") + ".xlsx";
            var fullPathfilePayment = Path.Combine(localDirectory, filePayment);
            if (File.Exists(fullPathfilePayment))
            {
                return Task.CompletedTask;
            }
            var fullPathfilePaymentSended = Path.Combine(_appSettings.Paths.LocalSendFile, filePayment);
            if (File.Exists(fullPathfilePaymentSended))
            {
                return Task.CompletedTask;
            }
            using (var sftp = new SftpClient(_appSettings.SFTP.Host, _appSettings.SFTP.Port, _appSettings.SFTP.Username, _appSettings.SFTP.Password))
            {
                sftp.ConnectionInfo.Timeout = TimeSpan.FromMinutes(2);
                sftp.Connect();
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
                            }
                        }
                        catch (Exception)
                        {
                            sftp.Disconnect();
                            sftp.Dispose();
                        }
                    }
                }
                sftp.Disconnect();
                sftp.Dispose();
            }
            return Task.CompletedTask;
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
            string remoteUPloadWorkingTime = _appSettings.RemotePaths.WorkingTime;
            string remoteUPloadCallReport = _appSettings.RemotePaths.CallReport;
            var fileNameCallReport = "call_report_" + sufixFile;
            var fullPathCallReport = Path.Combine(localFileCallReport, fileNameCallReport);
            var fileNameWorkingTimeReport = "working_time_" + sufixFile;
            var fullPathWorkingTimeReport = Path.Combine(localFileWorkingTime, fileNameWorkingTimeReport);
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

        public Task UploadFolderToSFCP()
        {
            var dateGet = DateTime.Now.ToString("yyyyMMdd");
            var fullPathWorkingTimeReport = Path.Combine(_appSettings.Paths.ToolCRMUploaWorkingTime, "working_time_" + dateGet + ".xlsx");
            var fullPathCallReport = Path.Combine(_appSettings.Paths.ToolCRMUploaCallReport, "call_report_" + dateGet + ".xlsx");
            var remoteUPloadWorkingTime = _appSettings.RemotePaths.WorkingTime;
            var remoteUPloadCallReport = _appSettings.RemotePaths.CallReport;
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
    }



}

