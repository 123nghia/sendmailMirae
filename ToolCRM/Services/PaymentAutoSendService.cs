using ToolCRM.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace ToolCRM.Services
{
    public class PaymentAutoSendService : BackgroundService
    {
        private readonly AppSettings _appSettings;
        private readonly ILogger<PaymentAutoSendService> _logger;
        private readonly ConcurrentDictionary<string, bool> _sentFiles;

        public PaymentAutoSendService(IOptions<AppSettings> appSettings, ILogger<PaymentAutoSendService> logger)
        {
            _appSettings = appSettings.Value;
            _logger = logger;
            _sentFiles = new ConcurrentDictionary<string, bool>();
            LoadSentFilesHistory();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendPaymentFiles();
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Kiểm tra mỗi 5 phút
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in PaymentAutoSendService");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Chờ 1 phút nếu có lỗi
                }
            }
        }

        private async Task CheckAndSendPaymentFiles()
        {
            try
            {
                using (var sftp = new Renci.SshNet.SftpClient(
                    _appSettings.Sftp.Host,
                    _appSettings.Sftp.Port,
                    _appSettings.Sftp.Username,
                    _appSettings.Sftp.Password))
                {
                    sftp.ConnectionInfo.Timeout = TimeSpan.FromMinutes(2);
                    sftp.Connect();

                    var files = sftp.ListDirectory(_appSettings.Sftp.PaymentFolder);
                    var paymentFiles = files.Where(f => f.IsRegularFile && f.Name.StartsWith("payment_")).ToList();

                    foreach (var file in paymentFiles)
                    {
                        var fileKey = GetFileKey(file);
                        
                        // Kiểm tra xem file đã được gửi chưa
                        if (!_sentFiles.ContainsKey(fileKey))
                        {
                            _logger.LogInformation($"New payment file detected: {file.Name}");
                            
                            // Download và gửi file
                            var success = await DownloadAndSendPaymentFile(sftp, file);
                            
                            if (success)
                            {
                                _sentFiles[fileKey] = true;
                                SaveSentFileToHistory(fileKey);
                                _logger.LogInformation($"Payment file sent successfully: {file.Name}");
                            }
                        }
                    }

                    sftp.Disconnect();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking payment files");
            }
        }

        private async Task<bool> DownloadAndSendPaymentFile(Renci.SshNet.SftpClient sftp, Renci.SshNet.Sftp.ISftpFile file)
        {
            try
            {
                var localDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Temp");
                if (!Directory.Exists(localDirectory))
                {
                    Directory.CreateDirectory(localDirectory);
                }

                var localFilePath = Path.Combine(localDirectory, file.Name);

                // Download file
                using (Stream fileStream = File.Create(localFilePath))
                {
                    sftp.DownloadFile(_appSettings.Sftp.PaymentFolder + "/" + file.Name, fileStream);
                }

                // Gửi email
                var sendmail = new ToolCRM.Business.Sendmail(
                    Microsoft.Extensions.Options.Options.Create(_appSettings));
                
                var result = await sendmail.SendPaymentFileEmail(localFilePath, file.Name, file.LastWriteTime);

                // Xóa file tạm
                if (File.Exists(localFilePath))
                {
                    File.Delete(localFilePath);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading and sending payment file: {file.Name}");
                return false;
            }
        }

        private string GetFileKey(Renci.SshNet.Sftp.ISftpFile file)
        {
            return $"{file.Name}_{file.LastWriteTime.Ticks}";
        }

        private void LoadSentFilesHistory()
        {
            try
            {
                var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), _appSettings.FilePaths.Logs);
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                
                var historyFile = Path.Combine(logDirectory, "payment_sent_history.txt");
                if (File.Exists(historyFile))
                {
                    var lines = File.ReadAllLines(historyFile);
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            _sentFiles[line] = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sent files history");
            }
        }

        private void SaveSentFileToHistory(string fileKey)
        {
            try
            {
                var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), _appSettings.FilePaths.Logs);
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                
                var historyFile = Path.Combine(logDirectory, "payment_sent_history.txt");
                File.AppendAllText(historyFile, fileKey + Environment.NewLine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving sent file to history");
            }
        }
    }
}
