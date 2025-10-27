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
            ViewBag.RootPath = "/";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ListDirectory(string path = "/")
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    path = "/";
                }

                var items = new List<dynamic>();

                using (var sftp = new SftpClient(
                    _appSettings.Sftp.Host,
                    _appSettings.Sftp.Port,
                    _appSettings.Sftp.Username,
                    _appSettings.Sftp.Password))
                {
                    sftp.ConnectionInfo.Timeout = TimeSpan.FromMinutes(2);
                    sftp.Connect();

                    try
                    {
                        var files = sftp.ListDirectory(path);
                        
                        foreach (var item in files)
                        {
                            if (item.Name == "." || item.Name == "..")
                                continue;

                            items.Add(new
                            {
                                name = item.Name,
                                fullPath = CombinePath(path, item.Name),
                                isDirectory = item.IsDirectory,
                                isFile = item.IsRegularFile,
                                size = item.Length,
                                lastModified = item.LastWriteTime,
                                permissions = item.Attributes.ToString()
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        return Json(new { success = false, message = ex.Message });
                    }

                    sftp.Disconnect();
                }

                return Json(new { success = true, path = path, items = items });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFileContent(string filePath)
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
                    
                    // Check file size before downloading
                    var fileInfo = sftp.Get(filePath);
                    if (fileInfo != null && fileInfo.Length > 10 * 1024 * 1024) // 10MB limit
                    {
                        return Json(new { success = false, message = "File is too large to preview (>10MB)" });
                    }

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

        private string CombinePath(string path1, string path2)
        {
            if (string.IsNullOrEmpty(path1))
                return path2;
            
            if (string.IsNullOrEmpty(path2))
                return path1;

            path1 = path1.TrimEnd('/');
            path2 = path2.TrimStart('/');

            if (path1 == "/")
                return "/" + path2;

            return path1 + "/" + path2;
        }
    }
}
