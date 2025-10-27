using Microsoft.AspNetCore.Mvc;
using Renci.SshNet;
using ToolCRM.Configuration;
using Microsoft.Extensions.Options;

namespace ToolCRM.Controllers
{
    public class SftpController : Controller
    {
        private readonly AppSettings _appSettings;

        public SftpController(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        public IActionResult Index()
        {
            ViewBag.SftpHost = _appSettings.Sftp.Host;
            ViewBag.SftpPort = _appSettings.Sftp.Port;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetFiles()
        {
            try
            {
                var files = new List<dynamic>();

                using (var sftp = new SftpClient(
                    _appSettings.Sftp.Host,
                    _appSettings.Sftp.Port,
                    _appSettings.Sftp.Username,
                    _appSettings.Sftp.Password))
                {
                    sftp.ConnectionInfo.Timeout = TimeSpan.FromMinutes(2);
                    sftp.Connect();

                    // Get files from WORKINGTIME folder
                    try
                    {
                        var workingTimeFiles = sftp.ListDirectory(_appSettings.Sftp.WorkingTimeFolder);
                        foreach (var file in workingTimeFiles.Where(f => f.IsRegularFile))
                        {
                            files.Add(new
                            {
                                name = file.Name,
                                path = _appSettings.Sftp.WorkingTimeFolder + file.Name,
                                size = file.Length,
                                lastModified = file.LastWriteTime,
                                type = "WorkingTime"
                            });
                        }
                    }
                    catch { }

                    // Get files from CallReport folder
                    try
                    {
                        var callReportFiles = sftp.ListDirectory(_appSettings.Sftp.CallReportFolder);
                        foreach (var file in callReportFiles.Where(f => f.IsRegularFile))
                        {
                            files.Add(new
                            {
                                name = file.Name,
                                path = _appSettings.Sftp.CallReportFolder + file.Name,
                                size = file.Length,
                                lastModified = file.LastWriteTime,
                                type = "CallReport"
                            });
                        }
                    }
                    catch { }

                    sftp.Disconnect();
                }

                return Json(new { success = true, files = files });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    return BadRequest("File path is required");
                }

                using (var sftp = new SftpClient(
                    _appSettings.Sftp.Host,
                    _appSettings.Sftp.Port,
                    _appSettings.Sftp.Username,
                    _appSettings.Sftp.Password))
                {
                    sftp.ConnectionInfo.Timeout = TimeSpan.FromMinutes(2);
                    sftp.Connect();

                    var fileName = Path.GetFileName(filePath);
                    var memoryStream = new MemoryStream();

                    sftp.DownloadFile(filePath, memoryStream);
                    memoryStream.Position = 0;

                    sftp.Disconnect();

                    return File(memoryStream.ToArray(), "application/octet-stream", fileName);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendLatestPayment()
        {
            try
            {
                var sendmail = new ToolCRM.Business.Sendmail(
                    Microsoft.Extensions.Options.Options.Create(_appSettings));
                
                var result = await sendmail.SendLatestPaymentFileAsync();

                if (result)
                {
                    return Json(new { success = true, message = "Payment file sent successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to send payment file" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
