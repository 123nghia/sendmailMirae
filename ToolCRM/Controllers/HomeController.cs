using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using ToolCRM.Business;
using ToolCRM.Configuration;
using ToolCRM.Models;
using Quartz;

namespace ToolCRM.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppSettings _appSettings;
        private readonly ISchedulerFactory _schedulerFactory;
        private HanldeBusiness bussines;

        public HomeController(ILogger<HomeController> logger, IOptions<AppSettings> appSettings, ISchedulerFactory schedulerFactory)
        {
            _logger = logger;
            _appSettings = appSettings.Value;
            _schedulerFactory = schedulerFactory;
            bussines = new HanldeBusiness(_appSettings, _schedulerFactory);
        }

        public async Task<IActionResult> Index(InputRequest? request)
        {
            if (request == null || request.DayReport.HasValue == false)
            {
                return View();
            }

            // Validate file uploads
            if (request.FileTC == null || request.FileTC.Length == 0)
            {
                ViewBag.ErrorMessage = "Vui lòng chọn file TC (File nhân viên)";
                return View("IndexError");
            }

            if (request.FileReport == null || request.FileReport.Length == 0)
            {
                ViewBag.ErrorMessage = "Vui lòng chọn file báo cáo (File CDR)";
                return View("IndexError");
            }

            try
            {
                var result = await bussines.MoveFileInputFormAsync(request);

                if(string.IsNullOrEmpty(result))
                {
                    return View("Success");
                }
                ViewBag.ErrorMessage = result;
                return View("IndexError");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file upload");
                ViewBag.ErrorMessage = $"Lỗi khi xử lý file: {ex.Message}";
                return View("IndexError");
            }
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> SendEmail()
        {
            try
            {
                await bussines.SendEmailReport();
                ViewBag.Message = "Email đã được gửi thành công!";
                return View("Success");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Lỗi khi gửi email: {ex.Message}";
                return View("IndexError");
            }
        }

        public async Task<IActionResult> SendLatestPaymentEmail()
        {
            try
            {
                await bussines.SendLatestPaymentEmail();
                ViewBag.Message = "Email gửi lại với file payment mới nhất thành công!";
                return View("Success");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Lỗi khi gửi lại email: {ex.Message}";
                return View("IndexError");
            }
        }

        public async Task<IActionResult> DownloadPayment()
        {
            try
            {
                await bussines.DownloadPaymentFile();
                ViewBag.Message = "File payment đã được tải về thành công!";
                return View("Success");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Lỗi khi tải file payment: {ex.Message}";
                return View("IndexError");
            }
        }

        public async Task<IActionResult> UploadToSFTP()
        {
            try
            {
                await bussines.UploadFilesToSFTP();
                ViewBag.Message = "File đã được upload lên SFTP thành công!";
                return View("Success");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Lỗi khi upload file: {ex.Message}";
                return View("IndexError");
            }
        }

        public IActionResult PaymentFiles()
        {
            try
            {
                var localDirectory = _appSettings.Paths.LocalFile;
                var paymentFiles = new List<object>();

                if (Directory.Exists(localDirectory))
                {
                    var files = Directory.GetFiles(localDirectory, "payment_*.xlsx")
                        .Select(f => new FileInfo(f))
                        .OrderByDescending(f => f.LastWriteTime)
                        .Select(f => new
                        {
                            Name = f.Name,
                            Size = FormatFileSize(f.Length),
                            LastModified = f.LastWriteTime.ToString("dd/MM/yyyy HH:mm"),
                            FullPath = f.FullName
                        })
                        .ToList();

                    paymentFiles = files.Cast<object>().ToList();
                }

                ViewBag.PaymentFiles = paymentFiles;
                ViewBag.LocalDirectory = localDirectory;
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Lỗi khi lấy danh sách file: {ex.Message}";
                return View("IndexError");
            }
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 B";

            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }
}