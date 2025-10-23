
using ToolCRM.Configuration;
using ToolCRM.Models;
using ToolCRM.Libraries.MiraeHandleLibrary;
using ToolCRM.Libraries.SendEmailLibrary;
using Quartz;

namespace ToolCRM.Business
{
    public class HanldeBusiness
    {
        public HandleFileWorkingTime handleFileWorkingTime;
        public HanleFileExcel hanleFileExcel;
        public ServiceSFCP serviceSFCP;
        public Sendmail sendmail;
        private readonly AppSettings _appSettings;
        private readonly ISchedulerFactory? _schedulerFactory;

        public HanldeBusiness(AppSettings appSettings, ISchedulerFactory? schedulerFactory = null)
        {
            _appSettings = appSettings;
            _schedulerFactory = schedulerFactory;
            handleFileWorkingTime = new HandleFileWorkingTime(_appSettings);
            hanleFileExcel = new HanleFileExcel(_appSettings);
            serviceSFCP = new ServiceSFCP(_appSettings);
            sendmail = new Sendmail(_appSettings);
        }


        public async Task<string> MoveFileInputFormAsync(InputRequest request)
        {
            var fileTC = request.FileTC;
            var fileReprort = request.FileReport;
            var dayReport = request.DayReport;
            var filePath = _appSettings.Paths.ToolCRMSourceFile;
            
            // Ensure directory exists
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
            
            var dateGet = (dayReport ?? DateTime.Now).ToString("yyyyMMdd");
            var workingTimeReportFile =  Path.Combine( filePath,  "working_time_" + dateGet + ".xlsx");
            var reprortCDRFileName = Path.Combine(filePath, "call_report_" + dateGet + ".xlsx");
            if(File.Exists(workingTimeReportFile))
            {
                File.Delete(workingTimeReportFile);
            }
            if (File.Exists(reprortCDRFileName))
            {
                File.Delete(reprortCDRFileName);
            }
            // Save FileTC
            if (fileTC != null && fileTC.Length > 0)
            {
                using (var stream = System.IO.File.Create(workingTimeReportFile))
                {
                    await fileTC.CopyToAsync(stream);
                }
            }
            else
            {
                return "File TC không hợp lệ hoặc rỗng";
            }

            // Save FileReport
            if (fileReprort != null && fileReprort.Length > 0)
            {
                using (var stream1 = System.IO.File.Create(reprortCDRFileName))
                {
                    await fileReprort.CopyToAsync(stream1);
                }
            }
            else
            {
                return "File báo cáo không hợp lệ hoặc rỗng";
            }

            // Validate files by checking if they exist and are not empty
            if (!File.Exists(workingTimeReportFile) || new FileInfo(workingTimeReportFile).Length == 0)
            {
                return "Kiểm tra file TC";
            }

            if (!File.Exists(reprortCDRFileName) || new FileInfo(reprortCDRFileName).Length == 0)
            {
                return "Kiểm tra file báo cáo";
            }
            
            handleFileWorkingTime.OutputFileWorkingTime();
            hanleFileExcel.OutPutFile();
            await serviceSFCP.UploadFolderToSFCP();
            await sendmail.send();

            // Trigger UploadFileReportJob after successful upload
            if (_schedulerFactory != null)
            {
                try
                {
                    var scheduler = await _schedulerFactory.GetScheduler();
                    var jobKey = new JobKey("UploadFileReportJob");
                    if (await scheduler.CheckExists(jobKey))
                    {
                        await scheduler.TriggerJob(jobKey);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the main process
                    Console.WriteLine($"Error triggering UploadFileReportJob: {ex.Message}");
                }
            }

            return string.Empty;
        }

        public async Task SendEmailReport()
        {
            await sendmail.send();
        }

        public async Task SendLatestPaymentEmail()
        {
            await sendmail.SendLatestPaymentEmail();
        }

        public async Task DownloadPaymentFile()
        {
            await serviceSFCP.DowloadFilePayment();
        }

        public async Task UploadFilesToSFTP()
        {
            await serviceSFCP.UploadFolderToSFCP();
        }
    }
}
