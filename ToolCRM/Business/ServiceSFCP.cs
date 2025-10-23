using Renci.SshNet;

namespace ToolCRM.Business
{
    public class ServiceSFCP
    {
        private readonly string HOST = @"smartbank-sftp.mafc.vn";
        private readonly string USERNAME = "smartbank";
        private readonly string PASSWORD = "$m@rT3anK2024";
        private readonly int PORT = 6336;
        public ServiceSFCP()
        {
        }
        private Task UploadFileWorkingTime( string filePath)
        {
            var dateHandle = DateTime.Now.AddDays(0);
            string remoteUPloadWorkingTime = "/uploads/WORKINGTIME/";
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
            return Task.CompletedTask;
        }

        private Task UploadFileReportCDR(string filePath)
        {
            string remoteUPloadCallReport = "/uploads/CAllREPORT/";
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
            return Task.CompletedTask;
        }
        private Task HandleFolderWorkingTime()
        {
          
            string localDirectory = "C:\\sendmailMirae\\ToolCRM\\UploadFile\\workingTime";
            var allFileInFolder = Directory.GetFiles(localDirectory);
            foreach (var item in allFileInFolder)
            {
                UploadFileWorkingTime(item);
            }
            return Task.CompletedTask;
        }
        private Task HandleFolderCDR()
        {
            string localDirectory = "C:\\sendmailMirae\\ToolCRM\\UploadFile\\CallReport";
            var allFileInFolder = Directory.GetFiles(localDirectory);
            foreach (var item in allFileInFolder)
            {
                UploadFileReportCDR(item);
            }
            return Task.CompletedTask;
        }
        public Task UploadFolderToSFCP()
        {
            HandleFolderWorkingTime();
            HandleFolderCDR();
            return Task.CompletedTask;
        }

        

    }



}

