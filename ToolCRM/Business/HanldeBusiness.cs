
using ToolCRM.Models;

namespace ToolCRM.Business
{
    public class HanldeBusiness
    {
        public HandleFileWorkingTime handleFileWorkingTime;
        public HanleFileExcel hanleFileExcel;

        public ServiceSFCP serviceSFCP;
        public HanldeBusiness()
        {
            handleFileWorkingTime = new HandleFileWorkingTime();
            hanleFileExcel = new HanleFileExcel();
            serviceSFCP = new ServiceSFCP();

        }


        public async Task<string> MoveFileInputFormAsync(InputRequest request)
        {
            var fileTC = request.FileTC;
            var fileReprort = request.FileReport;
            var dayReport = request.DayReport;
            var filePath = "C:\\sendmailMirae\\ToolCRM\\SourceFile";
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
            using (var stream = System.IO.File.Create(workingTimeReportFile))
            {
                if (fileTC != null)
                    await fileTC.CopyToAsync(stream);
                
            }
            using (var stream1 = System.IO.File.Create(reprortCDRFileName))
            {
                if (fileReprort != null)
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
