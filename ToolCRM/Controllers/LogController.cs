using Microsoft.AspNetCore.Mvc;
using ToolCRM.Models;

namespace ToolCRM.Controllers
{
    public class LogController : Controller
    {
        public IActionResult Index()
        {
            var model = new LogViewModel
            {
                EmailHistory = new List<LogEntry>(),
                UploadHistory = new List<LogEntry>(),
                DownloadHistory = new List<LogEntry>(),
                ProcessingHistory = new List<LogEntry>()
            };

            return View(model);
        }
    }
}
