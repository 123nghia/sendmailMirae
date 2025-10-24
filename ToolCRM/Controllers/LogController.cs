using Microsoft.AspNetCore.Mvc;
using ToolCRM.Configuration;
using ToolCRM.Services;
using ToolCRM.Models;
using Microsoft.Extensions.Options;

namespace ToolCRM.Controllers
{
    public class LogController : Controller
    {
        private readonly LoggingService _loggingService;

        public LogController(IOptions<AppSettings> appSettings)
        {
            _loggingService = new LoggingService(appSettings.Value);
        }

        public async Task<IActionResult> Index()
        {
            var model = new LogViewModel
            {
                EmailHistory = await _loggingService.GetEmailHistory(50),
                UploadHistory = await _loggingService.GetUploadHistory(50),
                DownloadHistory = await _loggingService.GetDownloadHistory(50),
                ProcessingHistory = await _loggingService.GetProcessingHistory(50)
            };

            return View(model);
        }

        public async Task<IActionResult> EmailHistory()
        {
            var history = await _loggingService.GetEmailHistory(100);
            return View(history);
        }

        public async Task<IActionResult> UploadHistory()
        {
            var history = await _loggingService.GetUploadHistory(100);
            return View(history);
        }

        public async Task<IActionResult> DownloadHistory()
        {
            var history = await _loggingService.GetDownloadHistory(100);
            return View(history);
        }

        public async Task<IActionResult> ProcessingHistory()
        {
            var history = await _loggingService.GetProcessingHistory(100);
            return View(history);
        }

        [HttpPost]
        public async Task<IActionResult> ClearOldLogs(int daysToKeep = 30)
        {
            await _loggingService.ClearOldLogs(daysToKeep);
            TempData["Message"] = $"Đã xóa log cũ hơn {daysToKeep} ngày";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadFile(string logType, int index)
        {
            try
            {
                List<LogEntry> entries = logType.ToLower() switch
                {
                    "upload" => await _loggingService.GetUploadHistory(100),
                    "download" => await _loggingService.GetDownloadHistory(100),
                    "processing" => await _loggingService.GetProcessingHistory(100),
                    _ => new List<LogEntry>()
                };

                if (index >= 0 && index < entries.Count)
                {
                    var entry = entries[index];
                    if (entry.CanDownload && !string.IsNullOrEmpty(entry.LocalPath) && System.IO.File.Exists(entry.LocalPath))
                    {
                        var fileBytes = await System.IO.File.ReadAllBytesAsync(entry.LocalPath);
                        var fileName = entry.GetDisplayName();
                        return File(fileBytes, "application/octet-stream", fileName);
                    }
                }

                TempData["ErrorMessage"] = "File không tồn tại hoặc không thể tải xuống";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi tải file: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }

    public class LogViewModel
    {
        public List<LogEntry> EmailHistory { get; set; } = new();
        public List<LogEntry> UploadHistory { get; set; } = new();
        public List<LogEntry> DownloadHistory { get; set; } = new();
        public List<LogEntry> ProcessingHistory { get; set; } = new();
    }
}
