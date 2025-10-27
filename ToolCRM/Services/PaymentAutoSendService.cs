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
                    await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken); // Kiểm tra mỗi 10 phút
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
                    
                    // Lọc file payment của ngày hôm nay
                    var today = DateTime.Now.Date;
                    var todayPattern = today.ToString("yyyyMMdd");
                    var todayFile = files.FirstOrDefault(f => f.IsRegularFile && 
                                                                   f.Name.StartsWith("payment_") && 
                                                                   f.Name.Contains(todayPattern));
                    
                    if (todayFile != null)
                    {
                        var fileKey = GetFileKey(todayFile);
                        
                        // Kiểm tra xem file đã được gửi chưa
                        if (!_sentFiles.ContainsKey(fileKey))
                        {
                            _logger.LogInformation($"New payment file detected for today: {todayFile.Name}");
                            
                            // Download và gửi file
                            var success = await DownloadAndSendPaymentFile(sftp, todayFile);
                            
                            if (success)
                            {
                                _sentFiles[fileKey] = true;
                                SaveSentFileToHistory(fileKey);
                                _logger.LogInformation($"Payment file sent successfully: {todayFile.Name}");
                            }
                        }
                        else
                        {
                            _logger.LogInformation($"Payment file for today already sent: {todayFile.Name}");
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"No payment file found for today ({todayPattern})");
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
            Stream fileStream = null;
            try
            {
                var localDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Temp");
                if (!Directory.Exists(localDirectory))
                {
                    Directory.CreateDirectory(localDirectory);
                }

                var localFilePath = Path.Combine(localDirectory, file.Name);

                // Download file
                fileStream = File.Create(localFilePath);
                sftp.DownloadFile(_appSettings.Sftp.PaymentFolder + "/" + file.Name, fileStream);
                fileStream.Close();
                fileStream.Dispose();
                fileStream = null;

                // Đợi một chút để đảm bảo file được ghi hoàn toàn
                await Task.Delay(500);

                // Gửi email
                var sendmail = new ToolCRM.Business.Sendmail(
                    Microsoft.Extensions.Options.Options.Create(_appSettings));
                
                var result = await sendmail.SendPaymentFileEmail(localFilePath, file.Name, file.LastAccessTime);

                // Đợi một chút trước khi xóa file để đảm bảo email đã được gửi
                await Task.Delay(1000);

                // Xóa file tạm với retry
                var retryCount = 0;
                while (retryCount < 3 && File.Exists(localFilePath))
                {
                    try
                    {
                        File.Delete(localFilePath);
                        break;
                    }
                    catch (IOException)
                    {
                        retryCount++;
                        await Task.Delay(1000);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading and sending payment file: {file.Name}");
                
                // Đảm bảo stream được đóng
                if (fileStream != null)
                {
                    try
                    {
                        fileStream.Close();
                        fileStream.Dispose();
                    }
                    catch { }
                }
                
                return false;
            }
        }

        private string GetFileKey(Renci.SshNet.Sftp.ISftpFile file)
        {
            // Sử dụng LastAccessTime (thời gian tạo file) thay vì LastWriteTime
            return $"{file.Name}_{file.LastAccessTime.Ticks}";
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
