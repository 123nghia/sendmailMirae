using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using ToolCRM.Business;
using ToolCRM.Configuration;
using ToolCRM.Models;
using ToolCRM.Services;

namespace ToolCRM.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppSettings _appSettings;
        private readonly LoggingService loggingService;
        private HanldeBusiness bussines;

        public HomeController(ILogger<HomeController> logger, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _appSettings = appSettings.Value;
            loggingService = new LoggingService(_appSettings);
            bussines = new HanldeBusiness(_appSettings);
        }

        public IActionResult Dashboard()
        {
            return View("Index");
        }

        public async Task<IActionResult> Index(InputRequest? request)
        {
            // If no request or no DayReport, show the upload form
            if (request == null || request.DayReport.HasValue == false)
            {
                return View("Index");
            }

            // Validate file uploads
            if (request.FileTC == null || request.FileTC.Length == 0)
            {
                ViewBag.ErrorMessage = "❌ Lỗi: Vui lòng chọn file TC (File nhân viên)";
                ViewBag.ErrorType = "VALIDATION_ERROR";
                ViewBag.ErrorDetails = "File TC không được để trống";
                return View("IndexError");
            }

            if (request.FileReport == null || request.FileReport.Length == 0)
            {
                ViewBag.ErrorMessage = "❌ Lỗi: Vui lòng chọn file báo cáo (File CDR)";
                ViewBag.ErrorType = "VALIDATION_ERROR";
                ViewBag.ErrorDetails = "File báo cáo không được để trống";
                return View("IndexError");
            }

            try
            {
                // Log operation start
                await loggingService.LogOperation("FILE_UPLOAD", "Upload và xử lý file", true, 
                    $"File TC: {request.FileTC.FileName}, File Report: {request.FileReport.FileName}, Date: {request.DayReport}");

                var result = await bussines.MoveFileInputFormAsync(request);

                if(string.IsNullOrEmpty(result))
                {
                    ViewBag.Message = "✅ Xử lý file thành công!";
                    ViewBag.MessageType = "SUCCESS";
                    ViewBag.MessageDetails = "File đã được xử lý và upload thành công";
                    await loggingService.LogOperation("FILE_UPLOAD", "Upload và xử lý file", true, "Thành công hoàn toàn");
                    return View("Success");
                }
                
                // Check if it's a warning (contains "Cảnh báo")
                if (result.Contains("Cảnh báo"))
                {
                    ViewBag.Message = "⚠️ " + result;
                    ViewBag.MessageType = "WARNING";
                    ViewBag.MessageDetails = "File đã được xử lý nhưng có một số cảnh báo";
                    await loggingService.LogOperation("FILE_UPLOAD", "Upload và xử lý file", true, result);
                    return View("Success");
                }
                
                // It's an actual error
                ViewBag.ErrorMessage = "❌ " + result;
                ViewBag.ErrorType = "PROCESSING_ERROR";
                ViewBag.ErrorDetails = "Lỗi trong quá trình xử lý file";
                await loggingService.LogOperation("FILE_UPLOAD", "Upload và xử lý file", false, result);
                return View("IndexError");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file upload");
                
                // Log detailed error
                await loggingService.LogError("FILE_UPLOAD_ERROR", ex.Message, ex.StackTrace, 
                    $"File TC: {request.FileTC?.FileName}, File Report: {request.FileReport?.FileName}, Date: {request.DayReport}");
                
                ViewBag.ErrorMessage = $"❌ Lỗi hệ thống: {ex.Message}";
                ViewBag.ErrorType = "SYSTEM_ERROR";
                ViewBag.ErrorDetails = "Lỗi không mong muốn trong hệ thống";
                ViewBag.StackTrace = ex.StackTrace;
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

        [HttpPost]
        public async Task<IActionResult> SendEmail()
        {
            try
            {
                await loggingService.LogOperation("EMAIL_SEND", "Gửi email báo cáo", true, "Bắt đầu gửi email");
                var result = await bussines.SendEmailReport();
                
                if (string.IsNullOrEmpty(result))
                {
                    await loggingService.LogOperation("EMAIL_SEND", "Gửi email báo cáo", true, "Thành công");
                    return Json(new { 
                        success = true, 
                        message = "✅ Gửi email báo cáo thành công!",
                        messageType = "SUCCESS",
                        details = "Email đã được gửi thành công"
                    });
                }
                else
                {
                    await loggingService.LogOperation("EMAIL_SEND", "Gửi email báo cáo", false, result);
                    return Json(new { 
                        success = false, 
                        message = "❌ " + result,
                        messageType = "ERROR",
                        details = "Lỗi khi gửi email báo cáo"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email");
                await loggingService.LogError("EMAIL_SEND_ERROR", ex.Message, ex.StackTrace, "Gửi email báo cáo");
                return Json(new { 
                    success = false, 
                    message = $"❌ Lỗi hệ thống khi gửi email: {ex.Message}",
                    messageType = "SYSTEM_ERROR",
                    details = "Lỗi không mong muốn trong hệ thống"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendLatestPaymentEmail()
        {
            try
            {
                await loggingService.LogOperation("EMAIL_SEND", "Gửi email payment", true, "Bắt đầu gửi email payment");
                var result = await bussines.SendLatestPaymentEmail();
                
                if (string.IsNullOrEmpty(result))
                {
                    await loggingService.LogOperation("EMAIL_SEND", "Gửi email payment", true, "Thành công");
                    return Json(new { 
                        success = true, 
                        message = "✅ Gửi email payment thành công!",
                        messageType = "SUCCESS",
                        details = "Email payment đã được gửi thành công"
                    });
                }
                else
                {
                    await loggingService.LogOperation("EMAIL_SEND", "Gửi email payment", false, result);
                    return Json(new { 
                        success = false, 
                        message = "❌ " + result,
                        messageType = "ERROR",
                        details = "Lỗi khi gửi email payment"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending latest payment email");
                await loggingService.LogError("EMAIL_SEND_ERROR", ex.Message, ex.StackTrace, "Gửi email payment");
                return Json(new { 
                    success = false, 
                    message = $"❌ Lỗi hệ thống khi gửi email payment: {ex.Message}",
                    messageType = "SYSTEM_ERROR",
                    details = "Lỗi không mong muốn trong hệ thống"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DownloadPayment()
        {
            try
            {
                await loggingService.LogOperation("FILE_DOWNLOAD", "Tải file payment", true, "Bắt đầu tải file payment");
                var result = await bussines.DownloadPaymentFile();
                
                if (string.IsNullOrEmpty(result))
                {
                    await loggingService.LogOperation("FILE_DOWNLOAD", "Tải file payment", true, "Thành công");
                    return Json(new { 
                        success = true, 
                        message = "✅ Tải file payment thành công!",
                        messageType = "SUCCESS",
                        details = "File payment đã được tải về thành công"
                    });
                }
                else
                {
                    await loggingService.LogOperation("FILE_DOWNLOAD", "Tải file payment", false, result);
                    return Json(new { 
                        success = false, 
                        message = "❌ " + result,
                        messageType = "ERROR",
                        details = "Lỗi khi tải file payment"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading payment file");
                await loggingService.LogError("FILE_DOWNLOAD_ERROR", ex.Message, ex.StackTrace, "Tải file payment");
                return Json(new { 
                    success = false, 
                    message = $"❌ Lỗi hệ thống khi tải file payment: {ex.Message}",
                    messageType = "SYSTEM_ERROR",
                    details = "Lỗi không mong muốn trong hệ thống"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadToSFTP()
        {
            try
            {
                await loggingService.LogOperation("SFTP_UPLOAD", "Upload lên SFTP", true, "Bắt đầu upload lên SFTP");
                var result = await bussines.UploadFilesToSFTP();
                
                if (string.IsNullOrEmpty(result))
                {
                    await loggingService.LogOperation("SFTP_UPLOAD", "Upload lên SFTP", true, "Thành công");
                    return Json(new { 
                        success = true, 
                        message = "✅ Upload lên SFTP thành công!",
                        messageType = "SUCCESS",
                        details = "File đã được upload lên SFTP thành công"
                    });
                }
                else
                {
                    await loggingService.LogOperation("SFTP_UPLOAD", "Upload lên SFTP", false, result);
                    return Json(new { 
                        success = false, 
                        message = "❌ " + result,
                        messageType = "ERROR",
                        details = "Lỗi khi upload lên SFTP"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading to SFTP");
                await loggingService.LogError("SFTP_UPLOAD_ERROR", ex.Message, ex.StackTrace, "Upload lên SFTP");
                return Json(new { 
                    success = false, 
                    message = $"❌ Lỗi hệ thống khi upload lên SFTP: {ex.Message}",
                    messageType = "SYSTEM_ERROR",
                    details = "Lỗi không mong muốn trong hệ thống"
                });
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