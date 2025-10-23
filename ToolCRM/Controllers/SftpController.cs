using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ToolCRM.Configuration;
using ToolCRM.Services;

namespace ToolCRM.Controllers
{
    public class SftpController : Controller
    {
        private readonly SftpBrowserService _sftpService;
        private readonly AppSettings _appSettings;
        private readonly ILogger<SftpController> _logger;

        public SftpController(SftpBrowserService sftpService, IOptions<AppSettings> appSettings, ILogger<SftpController> logger)
        {
            _sftpService = sftpService;
            _appSettings = appSettings.Value;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string path = "/")
        {
            try
            {
                if (string.IsNullOrEmpty(path) || path == "/")
                {
                    path = _appSettings.RemotePaths.Payment; // Default to payment folder
                }

                var directoryModel = await _sftpService.BrowseDirectoryAsync(path);
                
                ViewBag.CurrentPath = path;
                ViewBag.DirectoryModel = directoryModel;
                ViewBag.SftpHost = _appSettings.SFTP.Host;
                ViewBag.SftpPort = _appSettings.SFTP.Port;
                ViewBag.Username = _appSettings.SFTP.Username;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing SFTP directory: {Path}", path);
                ViewBag.ErrorMessage = $"Lỗi khi kết nối SFTP: {ex.Message}";
                return View();
            }
        }

        public async Task<IActionResult> Download(string filePath, string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(fileName))
                {
                    return BadRequest("File path and name are required");
                }

                var stream = await _sftpService.DownloadFileAsync(filePath);
                
                return File(stream, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file: {FilePath}", filePath);
                ViewBag.ErrorMessage = $"Lỗi khi tải file: {ex.Message}";
                return View("Index");
            }
        }

        public IActionResult Navigate(string path)
        {
            return RedirectToAction("Index", new { path });
        }
    }
}
