using Renci.SshNet;

namespace UploadFile.Remote
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
            var localFileWorkingTime = "C:\\sendmailMirae\\sendEmail\\ResourceMirae\\workingTime";
            var localFileCallReport = "C:\\sendmailMirae\\sendEmail\\ResourceMirae\\callReport";
            string remoteUPloadWorkingTime = "/uploads/WORKINGTIME/";
            string remoteUPloadCallReport = "/uploads/CAllREPORT/";
            var fileNameCallReport = "call_report_" + sufixFile;
            var fullPathCallReport = Path.Combine(localFileCallReport, fileNameCallReport);
            var fileNameWorkingTimeReport = "working_time_" + sufixFile;
            var fullPathWorkingTimeReport = Path.Combine(localFileWorkingTime, fileNameWorkingTimeReport);
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

