using MailKit.Net.Smtp;
using MimeKit;
using ToolCRM.Configuration;
using Microsoft.Extensions.Options;

namespace ToolCRM.Business
{
    public class Sendmail
    {
        private readonly AppSettings _appSettings;
        public Sendmail(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }
        public async Task SendEmailReportAsync()
        {
            var nameFileRun = "payment_" + DateTime.Now.ToString("yyyyMMdd") + ".xlsx";
            var localFile = Path.Combine(Directory.GetCurrentDirectory(), _appSettings.FilePaths.SourceFile);
            var localSendFile = Path.Combine(Directory.GetCurrentDirectory(), "ResourceMirae", "FileSended");
            
            var pathFileRun = Path.Combine(localFile, nameFileRun);
            var pathFileMove = Path.Combine(localSendFile, nameFileRun);

            // Ensure directory exists
            if (!Directory.Exists(localSendFile))
            {
                Directory.CreateDirectory(localSendFile);
            }

            if (File.Exists(pathFileMove))
            {
                return;
            }

            if (!File.Exists(pathFileRun))
            {
                return;
            }

            var monthtext = DateTime.Now.ToString("yyyy.MM.dd");
            var subjectmail = "[" + monthtext + "]" + "báo cáo payment hàng ngày";
            var message = new MimeMessage();
            var titleMail = "File PAYMENT ngày " + DateTime.Now.ToString("dd.MM.yyyy");

            message.From.Add(new MailboxAddress(titleMail, _appSettings.Email.Username));
            var recipientAddress = _appSettings.Email.Recipient;
            message.To.Add(new MailboxAddress("", recipientAddress));
            message.Subject = subjectmail;

            var multipart = new Multipart("mixed");
            multipart.Add(new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = "Dear Admin <br><br>" +
                "Dữ liệu payment hàng ngày của (đối tác) gửi qua <br><br>" +
                "Dữ liệu được tính đến thời điểm gửi mail." +
                "<br><br>Thanks, Admin"
            });

            var excelFilename = pathFileRun;
            var streams = new List<Stream>();

            var stream = File.OpenRead(excelFilename);
            var attachment = new MimePart("application",
                "vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                Content = new MimeContent(stream, ContentEncoding.Default),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                ContentTransferEncoding = ContentEncoding.Binary,
                FileName = Path.GetFileName(excelFilename)
            };

            streams.Add(stream);
            multipart.Add(attachment);
            message.Body = multipart;

            using (var client = new SmtpClient())
            {
                client.Connect(_appSettings.Email.SmtpHost, _appSettings.Email.SmtpPort, MailKit.Security.SecureSocketOptions.Auto);
                client.Authenticate(_appSettings.Email.Username, _appSettings.Email.Password);
                client.Send(message);
                client.Disconnect(true);
                Console.WriteLine("Mail daily report sent.");
            }

            foreach (var stream1 in streams)
                stream1.Dispose();

            File.Move(pathFileRun, pathFileMove);
            Console.WriteLine("File moved successfully.");
        }

        public async Task DowloadFilePaymentAsync()
        {
            var localDirectory = Path.Combine(Directory.GetCurrentDirectory(), _appSettings.FilePaths.SourceFile);
            var remoteDirectory = "/uploads/PAYMENT";
            var filePayment = "payment_" + DateTime.Now.ToString("yyyyMMdd") + ".xlsx";
            var fullPathfilePayment = Path.Combine(localDirectory, filePayment);

            if (File.Exists(fullPathfilePayment))
            {
                return;
            }

            var fullPathfilePaymentSended = Path.Combine(Directory.GetCurrentDirectory(), "ResourceMirae", "FileSended", filePayment);
            if (File.Exists(fullPathfilePaymentSended))
            {
                return;
            }

            using (var sftp = new Renci.SshNet.SftpClient(_appSettings.Sftp.Host, _appSettings.Sftp.Port, _appSettings.Sftp.Username, _appSettings.Sftp.Password))
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
                            using (Stream file1 = File.Create(Path.Combine(localDirectory, remoteFileName)))
                            {
                                sftp.DownloadFile(remoteDirectory + "/" + remoteFileName, file1);
                            }
                        }
                        catch (Exception e)
                        {
                            sftp.Disconnect();
                            sftp.Dispose();
                        }
                    }
                }

                sftp.Disconnect();
                sftp.Dispose();
            }
        }
    }
}
