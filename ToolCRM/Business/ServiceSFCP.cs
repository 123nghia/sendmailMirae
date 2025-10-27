using Renci.SshNet;
using ToolCRM.Configuration;

namespace ToolCRM.Business
{
    public class ServiceSFCP
    {
        private readonly AppSettings _appSettings;
        
        public ServiceSFCP(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }
        
        private string HOST => _appSettings.Sftp.Host;
        private string USERNAME => _appSettings.Sftp.Username;
        private string PASSWORD => _appSettings.Sftp.Password;
        private int PORT => _appSettings.Sftp.Port;
        private string WorkingTimeFolder => _appSettings.Sftp.WorkingTimeFolder;
        private string CallReportFolder => _appSettings.Sftp.CallReportFolder;
        private async Task UploadFileWorkingTime( string filePath)
        {
            var dateHandle = DateTime.Now.AddDays(0);
            string remoteUPloadWorkingTime = WorkingTimeFolder;
            var fullPathWorkingTimeReport = filePath;
            var streams = new List<Stream>();
            using (var sftp = new SftpClient(HOST, PORT, USERNAME, PASSWORD))
            {
                sftp.ConnectionInfo.Timeout = TimeSpan.FromMinutes(2);
                sftp.Connect();
                if (File.Exists(fullPathWorkingTimeReport))
                {
                    FileInfo fileWorkingTime = new FileInfo(fullPathWorkingTimeReport);
                    var fileStream = new FileStream(fileWorkingTime.FullName, FileMode.Open);
                    if (fileStream != null)
                    {
                        sftp.UploadFile(fileStream, remoteUPloadWorkingTime + fileWorkingTime.Name, true, null);
                        streams.Add(fileStream);
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

        }

        private async Task UploadFileReportCDR(string filePath)
        {
            string remoteUPloadCallReport = CallReportFolder;
            var fullPathWorkingTimeReport = filePath;
            var streams = new List<Stream>();
            using (var sftp = new SftpClient(HOST, PORT, USERNAME, PASSWORD))
            {
                sftp.ConnectionInfo.Timeout = TimeSpan.FromMinutes(2);
                sftp.Connect();
                if (File.Exists(fullPathWorkingTimeReport))
                {
                    FileInfo fileWorkingTime = new FileInfo(fullPathWorkingTimeReport);
                    var fileStream = new FileStream(fileWorkingTime.FullName, FileMode.Open);
                    if (fileStream != null)
                    {
                        sftp.UploadFile(fileStream, remoteUPloadCallReport + fileWorkingTime.Name,true, null);
                        streams.Add(fileStream);
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
        }
        private async Task HandleFolderWorkingTime()
        {
            string localDirectory = Path.Combine(Directory.GetCurrentDirectory(), _appSettings.FilePaths.UploadWorkingTime);
            var allFileInFolder = Directory.GetFiles(localDirectory);
            foreach (var item in allFileInFolder)
            {
                await UploadFileWorkingTime(item);
            }

        }
        private async Task HandleFolderCDR()
        {
            string localDirectory = Path.Combine(Directory.GetCurrentDirectory(), _appSettings.FilePaths.UploadCallReport);
            var allFileInFolder = Directory.GetFiles(localDirectory);
            foreach (var item in allFileInFolder)
            {
                await UploadFileReportCDR(item);
            }
        }
        public async Task  UploadFolderToSFCP()
        {
            await HandleFolderWorkingTime();
            await HandleFolderCDR();
        }

        

    }



}

