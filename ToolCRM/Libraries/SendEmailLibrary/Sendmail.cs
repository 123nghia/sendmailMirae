

using MailKit.Net.Smtp;
using MimeKit;
using ToolCRM.Configuration;


namespace ToolCRM.Libraries.SendEmailLibrary
{
    public class Sendmail
    {
        private readonly AppSettings _appSettings;

        public Sendmail(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }
        public Task send()
        {
            sendemailreport();
            return Task.CompletedTask;
        }
        public Task sendemailreport()
        {
            var nameFileRun = "payment_" + DateTime.Now.ToString("yyyyMMdd") + ".xlsx";
            var pathFileRun = Path.Combine(_appSettings.Paths.LocalFile, nameFileRun);
            var pathFileMove = Path.Combine(_appSettings.Paths.LocalSendFile, nameFileRun);
            if (File.Exists(pathFileMove))
            {
                return Task.CompletedTask;
            }
            if (!File.Exists(pathFileRun))
            {
                return Task.CompletedTask;
            }
            var monthtext = DateTime.Now.ToString("yyyy.mm.dd");
            var subjectmail = "[" + monthtext + "]" + "báo cáo payment hàng ngày";
            var message = new MimeMessage();
            var titleMail = "File PAYMENT ngày " + DateTime.Now.ToString("dd.MM.yyyy");
            message.From.Add(new MailboxAddress(titleMail, _appSettings.Email.FromEmail));
            message.To.Add(new MailboxAddress("", _appSettings.Email.ToEmail));
            
            // Add CC emails
            foreach (var ccEmail in _appSettings.Email.CCEmails)
            {
                message.Cc.Add(new MailboxAddress("", ccEmail));
            }
            message.Subject = subjectmail;
            var multipart = new Multipart("mixed");
            multipart.Add(new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = "Dear Admin  <br><br>" +
                " Dữ liệu payment hàng ngày của  ( đối tác) gửi qua <br><br>" +
                " Dữ liệu được tính đến thời điểm gửi mail. " +
                "<br><br> Thanks, Admin"
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
                client.Connect(_appSettings.Email.SmtpHost, _appSettings.Email.SmtpPort, true);
                client.Authenticate(_appSettings.Email.Username, _appSettings.Email.Password);
                client.Send(message);
                client.Disconnect(true);
                Console.WriteLine("mail dailly report.");
            }

            foreach (var stream1 in streams)
                stream1.Dispose();
            File.Move(pathFileRun, pathFileMove);
            Console.WriteLine("Have move file.");
            Console.Read();
            return Task.CompletedTask;
        }

        public Task SendLatestPaymentEmail()
        {
            try
            {
                // Tìm file payment mới nhất
                var localDirectory = _appSettings.Paths.LocalFile;
                var paymentFiles = Directory.GetFiles(localDirectory, "payment_*.xlsx")
                    .Where(f => File.Exists(f))
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToList();

                if (!paymentFiles.Any())
                {
                    Console.WriteLine("Không tìm thấy file payment nào");
                    return Task.CompletedTask;
                }

                var latestFile = paymentFiles.First();
                var fileName = latestFile.Name;
                var filePath = latestFile.FullName;

                // Tạo email
                var monthtext = DateTime.Now.ToString("yyyy.MM.dd");
                var subjectmail = "[" + monthtext + "] Báo cáo payment mới nhất";
                var message = new MimeMessage();
                var titleMail = "File PAYMENT mới nhất - " + DateTime.Now.ToString("dd.MM.yyyy");
                
                message.From.Add(new MailboxAddress(titleMail, _appSettings.Email.FromEmail));
                message.To.Add(new MailboxAddress("", _appSettings.Email.ToEmail));
                
                // Add CC emails nếu có
                foreach (var ccEmail in _appSettings.Email.CCEmails)
                {
                    message.Cc.Add(new MailboxAddress("", ccEmail));
                }
                
                message.Subject = subjectmail;
                
                var multipart = new Multipart("mixed");
                multipart.Add(new TextPart(MimeKit.Text.TextFormat.Html)
                {
                    Text = "Dear Admin,<br><br>" +
                           "Đây là file payment mới nhất được gửi lại theo yêu cầu.<br><br>" +
                           "File: " + fileName + "<br>" +
                           "Thời gian tạo: " + latestFile.LastWriteTime.ToString("dd/MM/yyyy HH:mm") + "<br><br>" +
                           "Thanks,<br>Admin"
                });

                // Đính kèm file
                var stream = File.OpenRead(filePath);
                var attachment = new MimePart("application",
                   "vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                {
                    Content = new MimeContent(stream, ContentEncoding.Default),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Binary,
                    FileName = fileName
                };

                multipart.Add(attachment);
                message.Body = multipart;

                // Gửi email
                using (var client = new SmtpClient())
                {
                    client.Connect(_appSettings.Email.SmtpHost, _appSettings.Email.SmtpPort, true);
                    client.Authenticate(_appSettings.Email.Username, _appSettings.Email.Password);
                    client.Send(message);
                    client.Disconnect(true);
                    Console.WriteLine("Email gửi lại thành công với file: " + fileName);
                }

                stream.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi gửi email: " + ex.Message);
            }

            return Task.CompletedTask;
        }
    }
}
