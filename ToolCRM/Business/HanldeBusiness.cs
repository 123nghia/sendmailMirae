
using ToolCRM.Models;
using ToolCRM.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace ToolCRM.Business
{
    public class HanldeBusiness
    {
        public HandleFileWorkingTime handleFileWorkingTime;
        public HanleFileExcel hanleFileExcel;
        public ServiceSFCP serviceSFCP;
        public Sendmail sendmail;
        
        private readonly AppSettings _appSettings;
        private readonly ILogger<ServiceSFCP> _logger;
        
        public HanldeBusiness(IOptions<AppSettings> appSettings, ILogger<ServiceSFCP> logger)
        {
            _appSettings = appSettings.Value;
            _logger = logger;
            handleFileWorkingTime = new HandleFileWorkingTime(_appSettings);
            hanleFileExcel = new HanleFileExcel(_appSettings);
            serviceSFCP = new ServiceSFCP(_appSettings, logger);
            sendmail = new Sendmail(appSettings);
        }


        public async Task<string> MoveFileInputFormAsync(InputRequest request)
        {
            var fileTC = request.FileTC;
            var fileReprort = request.FileReport;
            var dayReport = request.DayReport;
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), _appSettings.FilePaths.SourceFile);
            var dateGet = dayReport.Value.ToString("yyyyMMdd");
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
            using (var stream = System.IO.File.Create(workingTimeReportFile))
            {
                await fileTC.CopyToAsync(stream);
                
            }
            using (var stream1 = System.IO.File.Create(reprortCDRFileName))
            {
                await fileReprort.CopyToAsync(stream1);

            }

            if(!handleFileWorkingTime.CheckValidFile(workingTimeReportFile))
            {
                return "Kiểm tra file TC";
            }

            if (!hanleFileExcel.CheckValidFile(reprortCDRFileName))
            {
                return "Kiểm tra file báo cáo";
            }
            
            _logger.LogInformation("Starting to process files for upload...");
            
            handleFileWorkingTime.OutputFileWorkingTime(dayReport, workingTimeReportFile);
            _logger.LogInformation("WorkingTime file created successfully");
         
            hanleFileExcel.OutPutFile(dayReport, reprortCDRFileName);
            _logger.LogInformation("CallReport file created successfully");
            
            await serviceSFCP.UploadFolderToSFCP();
            _logger.LogInformation("SFTP upload completed");

            return string.Empty;
        }



    }
}
