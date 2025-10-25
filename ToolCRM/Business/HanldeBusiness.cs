
using ToolCRM.Configuration;
using ToolCRM.Models;
using ToolCRM.Libraries.MiraeHandleLibrary;
using ToolCRM.Libraries.SendEmailLibrary;
using ToolCRM.Services;

namespace ToolCRM.Business
{
    public class HanldeBusiness
    {
        public HandleFileWorkingTime handleFileWorkingTime;
        public HanleFileExcel hanleFileExcel;
        public ServiceSFCP serviceSFCP;
        public Sendmail sendmail;
        private readonly AppSettings _appSettings;
        private readonly LoggingService loggingService;

        public HanldeBusiness(AppSettings appSettings)
        {
            _appSettings = appSettings;
            handleFileWorkingTime = new HandleFileWorkingTime(_appSettings);
            hanleFileExcel = new HanleFileExcel(_appSettings);
            serviceSFCP = new ServiceSFCP(_appSettings);
            sendmail = new Sendmail(_appSettings);
            loggingService = new LoggingService(_appSettings);
        }


        public async Task<string> MoveFileInputFormAsync(InputRequest request)
        {
            Console.WriteLine("🚀 Bắt đầu xử lý file input form...");
            
            var fileTC = request.FileTC;
            var fileReprort = request.FileReport;
            var dayReport = request.DayReport;
            var filePath = _appSettings.Paths.ToolCRMSourceFile;
            var dateGet = (dayReport ?? DateTime.Now).ToString("yyyyMMdd");
            var tcFilePath = Path.Combine(filePath, "tc_" + dateGet + ".xlsx");
            var workingTimeReportFile = Path.Combine(filePath, "working_time_" + dateGet + ".xlsx");
            var reprortCDRFileName = Path.Combine(filePath, "call_report_" + dateGet + ".xlsx");
            
            Console.WriteLine($"📅 Ngày báo cáo: {dateGet}");
            Console.WriteLine($"📁 Đường dẫn file: {filePath}");
            Console.WriteLine($"📄 File TC: {tcFilePath}");
            Console.WriteLine($"📄 File Working Time: {workingTimeReportFile}");
            Console.WriteLine($"📄 File Call Report: {reprortCDRFileName}");
            
            var processSteps = new List<string>();
            var errors = new List<string>();
            
            try
            {
                // Step 1: Ensure directory exists
                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                    processSteps.Add("✓ Tạo thư mục lưu trữ");
                }
                else
                {
                    processSteps.Add("✓ Thư mục lưu trữ đã tồn tại");
                }
                
                // Step 2: Delete existing files
                if (File.Exists(workingTimeReportFile))
            {
                File.Delete(workingTimeReportFile);
                    processSteps.Add("✓ Xóa file working time cũ");
            }
            if (File.Exists(reprortCDRFileName))
            {
                File.Delete(reprortCDRFileName);
                    processSteps.Add("✓ Xóa file call report cũ");
                }
                
                // Step 3: Save FileTC
                Console.WriteLine("📤 Bước 3: Lưu file TC...");
                if (fileTC != null && fileTC.Length > 0)
                {
                    Console.WriteLine($"📄 File TC size: {fileTC.Length} bytes");
                    using (var stream = System.IO.File.Create(tcFilePath))
                    {
                        await fileTC.CopyToAsync(stream);
                    }
                    Console.WriteLine($"✅ Đã lưu file TC: {tcFilePath}");
                    processSteps.Add("✓ Lưu file TC");
                }
                else
                {
                    Console.WriteLine("❌ File TC không hợp lệ hoặc rỗng");
                    errors.Add("❌ File TC không hợp lệ hoặc rỗng");
                    await loggingService.LogFileProcessing("tc_" + dateGet + ".xlsx", "FileUpload", false, "File TC không hợp lệ hoặc rỗng");
                    return string.Join("; ", errors);
                }

                // Step 4: Save FileReport
                Console.WriteLine("📤 Bước 4: Lưu file Report...");
                if (fileReprort != null && fileReprort.Length > 0)
                {
                    Console.WriteLine($"📄 File Report size: {fileReprort.Length} bytes");
                    using (var stream1 = System.IO.File.Create(reprortCDRFileName))
                    {
                        await fileReprort.CopyToAsync(stream1);
                    }
                    Console.WriteLine($"✅ Đã lưu file Report: {reprortCDRFileName}");
                    processSteps.Add("✓ Lưu file báo cáo (Call Report)");
                }
                else
                {
                    Console.WriteLine("⚠️ File Report không có - bỏ qua bước này");
                    processSteps.Add("⚠️ Không có file Report - bỏ qua");
                    // Không return nữa, tiếp tục xử lý
                }

                // Step 5: Validate files
                Console.WriteLine("🔍 Bước 5: Xác thực file TC...");
                if (!File.Exists(tcFilePath) || new FileInfo(tcFilePath).Length == 0)
                {
                    Console.WriteLine("❌ File TC không tồn tại hoặc rỗng sau khi lưu");
                    errors.Add("❌ File TC không tồn tại hoặc rỗng sau khi lưu");
                    await loggingService.LogFileProcessing("tc_" + dateGet + ".xlsx", "FileValidation", false, "File không tồn tại hoặc rỗng");
                    return string.Join("; ", errors);
                }
                else
                {
                    Console.WriteLine("✅ File TC hợp lệ");
                    processSteps.Add("✓ Xác thực file TC");
                }

                // Validate file Report (optional)
                if (fileReprort != null && fileReprort.Length > 0)
                {
                    if (!File.Exists(reprortCDRFileName) || new FileInfo(reprortCDRFileName).Length == 0)
                    {
                        Console.WriteLine("❌ File báo cáo không tồn tại hoặc rỗng sau khi lưu");
                        errors.Add("❌ File báo cáo không tồn tại hoặc rỗng sau khi lưu");
                        await loggingService.LogFileProcessing("call_report_" + dateGet + ".xlsx", "FileValidation", false, "File không tồn tại hoặc rỗng");
                        return string.Join("; ", errors);
                    }
                    else
                    {
                        Console.WriteLine("✅ File Report hợp lệ");
                        processSteps.Add("✓ Xác thực file báo cáo");
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ Không có file Report để xác thực");
                }
                
                // Step 6: Process files
                Console.WriteLine("⚙️ Bước 6: Xử lý file Working Time...");
                try
                {
                    Console.WriteLine($"📖 Đọc dữ liệu từ file TC: {tcFilePath}");
                    handleFileWorkingTime.OutputFileWorkingTime(tcFilePath);
                    Console.WriteLine($"✅ Đã tạo file Working Time: {workingTimeReportFile}");
                    processSteps.Add("✓ Xử lý file Working Time");
                    await loggingService.LogFileProcessing("working_time_" + dateGet + ".xlsx", "FileProcessing", true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Lỗi khi xử lý file Working Time: {ex.Message}");
                    errors.Add($"❌ Lỗi khi xử lý file Working Time: {ex.Message}");
                    await loggingService.LogFileProcessing("working_time_" + dateGet + ".xlsx", "FileProcessing", false, ex.Message);
                }
                
                try
                {
                    hanleFileExcel.OutPutFile();
                    processSteps.Add("✓ Xử lý file Call Report");
                    await loggingService.LogFileProcessing("call_report_" + dateGet + ".xlsx", "FileProcessing", true);
                }
                catch (Exception ex)
                {
                    errors.Add($"❌ Lỗi khi xử lý file Call Report: {ex.Message}");
                    await loggingService.LogFileProcessing("call_report_" + dateGet + ".xlsx", "FileProcessing", false, ex.Message);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"❌ Lỗi chung khi xử lý file: {ex.Message}");
                await loggingService.LogFileProcessing("file_processing", "GeneralError", false, ex.Message);
                return $"Lỗi chung: {ex.Message}";
            }

            // Step 7: Upload to SFTP (non-blocking)
            string sftpError = null;
            try
            {
                await serviceSFCP.UploadFolderToSFCP();
                processSteps.Add("✓ Upload file lên SFTP");
                
                // Log successful upload
                await loggingService.LogFileUpload(
                    "working_time_" + dateGet + ".xlsx", 
                    "WorkingTime", 
                    workingTimeReportFile, 
                    "SFTP", 
                    true
                );
                await loggingService.LogFileUpload(
                    "call_report_" + dateGet + ".xlsx", 
                    "CallReport", 
                    reprortCDRFileName, 
                    "SFTP", 
                    true
                );
            }
            catch (Exception ex)
            {
                sftpError = $"❌ Lỗi upload SFTP: {ex.Message}";
                errors.Add(sftpError);
                processSteps.Add("❌ Upload SFTP thất bại");
                Console.WriteLine($"SFTP Upload Error: {ex.Message}");
                
                // Log failed upload
                await loggingService.LogFileUpload(
                    "working_time_" + dateGet + ".xlsx", 
                    "WorkingTime", 
                    workingTimeReportFile, 
                    "SFTP", 
                    false, 
                    ex.Message
                );
                await loggingService.LogFileUpload(
                    "call_report_" + dateGet + ".xlsx", 
                    "CallReport", 
                    reprortCDRFileName, 
                    "SFTP", 
                    false, 
                    ex.Message
                );
            }

            // Step 8: Send email (non-blocking)
            string emailError = null;
            try
            {
                await sendmail.send();
                processSteps.Add("✓ Gửi email báo cáo");
                
                // Log successful email
                await loggingService.LogEmailSent(
                    "DailyReport", 
                    _appSettings.Email.ToEmail, 
                    "Báo cáo hàng ngày - " + dateGet, 
                    true
                );
            }
            catch (Exception ex)
            {
                emailError = $"❌ Lỗi gửi email: {ex.Message}";
                errors.Add(emailError);
                processSteps.Add("❌ Gửi email thất bại");
                Console.WriteLine($"Email Send Error: {ex.Message}");
                
                // Log failed email
                await loggingService.LogEmailSent(
                    "DailyReport", 
                    _appSettings.Email.ToEmail, 
                    "Báo cáo hàng ngày - " + dateGet, 
                    false, 
                    ex.Message
                );
            }

            // Log successful file processing
            await loggingService.LogFileProcessing(
                "working_time_" + dateGet + ".xlsx", 
                "FileProcessing", 
                true
            );
            await loggingService.LogFileProcessing(
                "call_report_" + dateGet + ".xlsx", 
                "FileProcessing", 
                true
            );

            // Build detailed result message
            var resultMessage = new List<string>();
            
            // Add successful steps
            if (processSteps.Any())
            {
                resultMessage.Add("Các bước đã thực hiện:");
                resultMessage.AddRange(processSteps);
            }
            
            // Add errors if any
            if (errors.Any())
            {
                resultMessage.Add("");
                resultMessage.Add("Lỗi gặp phải:");
                resultMessage.AddRange(errors);
            }
            
            // Determine overall status
            if (errors.Any())
            {
                return string.Join("\n", resultMessage);
            }
            else
            {
                return $"✅ Xử lý hoàn tất thành công!\n\n{string.Join("\n", processSteps)}";
            }
        }

        public async Task<string> SendEmailReport()
        {
            try
            {
                await sendmail.send();
                
                // Log successful email
                await loggingService.LogEmailSent(
                    "ManualReport", 
                    _appSettings.Email.ToEmail, 
                    "Báo cáo thủ công", 
                    true
                );
                
                return "✅ Gửi email báo cáo thành công!";
            }
            catch (Exception ex)
            {
                // Log failed email
                await loggingService.LogEmailSent(
                    "ManualReport", 
                    _appSettings.Email.ToEmail, 
                    "Báo cáo thủ công", 
                    false, 
                    ex.Message
                );
                
                return $"❌ Lỗi khi gửi email: {ex.Message}";
            }
        }

        public async Task<string> SendLatestPaymentEmail()
        {
            try
            {
                await sendmail.SendLatestPaymentEmail();
                
                // Log successful email
                await loggingService.LogEmailSent(
                    "PaymentEmail", 
                    _appSettings.Email.ToEmail, 
                    "Email Payment mới nhất", 
                    true
                );
                
                return "✅ Gửi email payment thành công!";
            }
            catch (Exception ex)
            {
                // Log failed email
                await loggingService.LogEmailSent(
                    "PaymentEmail", 
                    _appSettings.Email.ToEmail, 
                    "Email Payment mới nhất", 
                    false, 
                    ex.Message
                );
                
                return $"❌ Lỗi khi gửi email payment: {ex.Message}";
            }
        }

        public async Task<string> DownloadPaymentFile()
        {
            try
            {
                var (hasNewFile, fileName) = await serviceSFCP.DowloadFilePayment();
                
                if (hasNewFile)
                {
                    // Log successful download
                    var localPath = Path.Combine(_appSettings.Paths.LocalFile, fileName);
                    await loggingService.LogFileDownload(
                        fileName, 
                        "SFTP", 
                        localPath, 
                        true
                    );
                    
                    // Tự động gửi email với file payment mới
                    try
                    {
                        await sendmail.SendLatestPaymentEmail();
                        
                        // Log successful email
                        await loggingService.LogEmailSent(
                            "PaymentEmail", 
                            _appSettings.Email.ToEmail, 
                            "File Payment mới - " + fileName, 
                            true
                        );
                        
                        return $"✅ Tải file payment thành công: {fileName}\n✅ Email đã được gửi tự động!";
                    }
                    catch (Exception emailEx)
                    {
                        // Log failed email
                        await loggingService.LogEmailSent(
                            "PaymentEmail", 
                            _appSettings.Email.ToEmail, 
                            "File Payment mới - " + fileName, 
                            false, 
                            emailEx.Message
                        );
                        
                        return $"✅ Tải file payment thành công: {fileName}\n❌ Cảnh báo: Không thể gửi email tự động - {emailEx.Message}";
                    }
                }
                else
                {
                    return "ℹ️ Không có file payment mới để tải về.";
                }
            }
            catch (Exception ex)
            {
                // Log failed download
                await loggingService.LogFileDownload(
                    "payment_file", 
                    "SFTP", 
                    "", 
                    false, 
                    ex.Message
                );
                
                return $"❌ Lỗi khi tải file payment: {ex.Message}";
            }
        }

        public async Task<string> UploadFilesToSFTP()
        {
            try
            {
                await serviceSFCP.UploadFolderToSFCP();
                
                // Log successful upload
                await loggingService.LogFileUpload(
                    "folder_upload", 
                    "Folder", 
                    _appSettings.Paths.ToolCRMUploaWorkingTime, 
                    "SFTP", 
                    true
                );
                
                return "✅ Upload file lên SFTP thành công!";
            }
            catch (Exception ex)
            {
                // Log failed upload
                await loggingService.LogFileUpload(
                    "folder_upload", 
                    "Folder", 
                    _appSettings.Paths.ToolCRMUploaWorkingTime, 
                    "SFTP", 
                    false, 
                    ex.Message
                );
                
                return $"❌ Lỗi khi upload lên SFTP: {ex.Message}";
            }
        }
    }
}
