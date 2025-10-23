
using Microsoft.AspNetCore.Mvc.RazorPages;

using UploadFile.Model;

namespace UploadFile.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {

        }

        public void OnPostUploadInfomation(FileInfoRequest request)
        {
            var uploadDirecotroy = "C:\\sendmailMirae\\UploadFile\\RessourceFile\\File";

            var dateHandle = DateTime.Now.AddDays(0);
            if (dateHandle.DayOfWeek == DayOfWeek.Monday)
            {
                dateHandle = dateHandle.AddDays(-2);
            }
            else
                dateHandle = dateHandle.AddDays(-1);
            var fileInfo = request.FileInfo;

            if (!Directory.Exists(uploadDirecotroy))
                Directory.CreateDirectory(uploadDirecotroy);
            var sufixFile = dateHandle.ToString("yyyyMMdd") + ".xlsx";
            var fileNameCallReport = "call_report_" + sufixFile;
            var filePath = Path.Combine(uploadDirecotroy, fileNameCallReport);
            using (var strem = System.IO.File.Create(filePath))
            {
                fileInfo?.CopyTo(strem);
            }

        }
    }
}