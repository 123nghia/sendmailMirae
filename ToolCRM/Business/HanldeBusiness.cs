
using ToolCRM.Models;
using ToolCRM.Configuration;
using Microsoft.Extensions.Options;

namespace ToolCRM.Business
{
    public class HanldeBusiness
    {
        public HandleFileWorkingTime handleFileWorkingTime;
        public HanleFileExcel hanleFileExcel;
        public ServiceSFCP serviceSFCP;
        public Sendmail sendmail;
        
        private readonly AppSettings _appSettings;
        
        public HanldeBusiness(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
            handleFileWorkingTime = new HandleFileWorkingTime(_appSettings);
            hanleFileExcel = new HanleFileExcel(_appSettings);
            serviceSFCP = new ServiceSFCP(_appSettings);
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
            handleFileWorkingTime.OutputFileWorkingTime(dayReport, workingTimeReportFile);
         
            hanleFileExcel.OutPutFile(dayReport, reprortCDRFileName);
            await serviceSFCP.UploadFolderToSFCP();

            return string.Empty;
        }



    }
}
