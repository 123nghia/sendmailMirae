using ToolCRM.Libraries.MiraeHandleLibrary.Model;
using ToolCRM.Configuration;
using OfficeOpenXml;

namespace ToolCRM.Libraries.MiraeHandleLibrary
{
    public class HandleFileWorkingTime
    {
        private readonly AppSettings _appSettings;
        public readonly string FORTMAT_DATETIME = "mmmm d, yyyy, h:mm AM/PM";

        public HandleFileWorkingTime(AppSettings? appSettings = null)
        {
            _appSettings = appSettings ?? new AppSettings();
        }
        public List<DataWorkingTimeSource> LoadFileDataSorce(string fileTcPath)
        {
            Console.WriteLine($"🔍 LoadFileDataSorce: Bắt đầu đọc file TC từ {fileTcPath}");
            var listData = new List<DataWorkingTimeSource>();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            if (string.IsNullOrEmpty(fileTcPath) || !File.Exists(fileTcPath))
            {
                Console.WriteLine($"❌ File TC không tồn tại: {fileTcPath}");
                throw new FileNotFoundException($"File TC không tồn tại: {fileTcPath}");
            }
            
            Console.WriteLine($"📖 Đang đọc file Excel: {fileTcPath}");
            using (ExcelPackage package = new ExcelPackage(new FileInfo(fileTcPath)))
            {
                ExcelWorksheet? workSheet = package.Workbook.Worksheets.FirstOrDefault();
                if (workSheet != null)
                {
                    int totalRows = workSheet.Rows.Count();
                    Console.WriteLine($"📊 Tổng số dòng trong file: {totalRows}");
                    
                    for (int i = 2; i <= totalRows; i++)
                    {
                        var item = new DataWorkingTimeSource();
                        item.UserName = workSheet.Cells[i, 1].Value?.ToString() ?? string.Empty; // Đọc cột A thay vì cột C
                        listData.Add(item);
                        Console.WriteLine($"👤 Dòng {i}: UserName = {item.UserName}");
                    }
                }
                else
                {
                    Console.WriteLine("❌ Không tìm thấy worksheet trong file Excel");
                }
            }
            Console.WriteLine($"✅ Đã đọc được {listData.Count} records từ file TC");
            return listData;
        }

        public DateTime? GetDateTime(string dayText)
        {

            if (string.IsNullOrEmpty(dayText))
            {
                return DateTime.MinValue;
            }
            return DateTime.FromOADate(int.Parse(dayText));
        }
        private string Gettime(string timeText)
        {

            if (string.IsNullOrEmpty(timeText))
            {
                return "";
            }
            try
            {
                var datetimeInput = DateTime.Parse(timeText);

                return datetimeInput.TimeOfDay.ToString();
            }
            catch (Exception)
            {

                return "";
            }

        }


        public static DateTime GetRandomDateTime(DateTime? min = null, DateTime? max = null)
        {
            Random rnd = new Random();
            var range = (max ?? DateTime.Now) - (min ?? DateTime.MinValue);
            var randomUpperBound = (Int32)range.TotalSeconds;
            if (randomUpperBound <= 0)
                randomUpperBound = rnd.Next(1, Int32.MaxValue);

            var randTimeSpan = TimeSpan.FromSeconds((Int64)(range.TotalSeconds - rnd.Next(0, randomUpperBound)));
            return (min ?? DateTime.MinValue).Add(randTimeSpan);
        }
        public void OutputFileWorkingTime(string fileTcPath)
        {
            Console.WriteLine($"🏭 OutputFileWorkingTime: Bắt đầu tạo file Working Time từ {fileTcPath}");
            
            var timeNow = DateTime.Now.AddDays(-1);
            Console.WriteLine($"📅 Thời gian báo cáo: {timeNow:yyyy-MM-dd}");
            
            var listDataHandle = LoadFileDataSorce(fileTcPath);
            Console.WriteLine($"📊 Số lượng nhân viên: {listDataHandle.Count}");
            
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var pathInfo = _appSettings.Paths.ToolCRMUploaWorkingTime;
            var dateGet = timeNow.ToString("yyyyMMdd");
            var fileName = "working_time_" + dateGet + ".xlsx";
            var fileInfo = Path.Combine(pathInfo, fileName);
            
            Console.WriteLine($"📁 Đường dẫn file output: {fileInfo}");
            
            if (File.Exists(fileInfo))
            {
                Console.WriteLine("🗑️ Xóa file cũ...");
                File.Delete(fileInfo);
            }
            
            var file = new FileInfo(fileInfo);
            using (ExcelPackage package = new ExcelPackage(file))
            {
                Console.WriteLine("📝 Tạo worksheet mới...");
                var sheet = package.Workbook.Worksheets.Add("Sheet1");
                sheet.Cells[1, 1].Value = "USERNAME";
                sheet.Cells[1, 2].Value = "CHECK_IN";
                sheet.Cells[1, 3].Value = "CHECK_OUT";
                sheet.Cells[1, 4].Value = "DURATION_IN_HOUR";
                sheet.Cells[1, 5].Value = "WEEK_DAY";
                
                var indexLoop = 2;
                Console.WriteLine("⏰ Tạo dữ liệu working time cho từng nhân viên...");
                
                foreach (var item in listDataHandle)
                {
                    sheet.Cells[indexLoop, 1].Value = item.UserName;
                    var day = timeNow.Day;
                    var month = timeNow.Month;
                    var year = timeNow.Year;
                    var timeLoginStand = new DateTime(year, month, day, 08, 30, 00);
                    var timeLogoutStand = new DateTime(year, month, day, 17, 30, 00);
                    var timeLogin = GetRandomDateTime(timeLoginStand.AddMinutes(-10), timeLoginStand.AddMinutes(2));
                    var timeLogout = GetRandomDateTime(timeLogoutStand.AddMinutes(-3), timeLogoutStand.AddMinutes(5));

                    var hours = (timeLogout - timeLogin).TotalHours;
                    hours = Math.Round(hours, 1);
                    sheet.Cells[indexLoop, 2].Style.Numberformat.Format = FORTMAT_DATETIME;
                    sheet.Cells[indexLoop, 2].Value = timeLogin;
                    sheet.Cells[indexLoop, 3].Style.Numberformat.Format = FORTMAT_DATETIME;
                    sheet.Cells[indexLoop, 3].Value = timeLogout;
                    sheet.Cells[indexLoop, 4].Value = hours;
                    sheet.Cells[indexLoop, 5].Value = timeNow.DayOfWeek.ToString();
                    
                    Console.WriteLine($"👤 {item.UserName}: {timeLogin:HH:mm} - {timeLogout:HH:mm} ({hours}h)");
                    indexLoop++;
                }
                
                Console.WriteLine("📐 Auto-fit columns...");
                sheet.Cells.AutoFitColumns();
                
                Console.WriteLine("💾 Lưu file...");
                package.Save();
            }
            
            Console.WriteLine($"✅ Hoàn thành tạo file Working Time: {fileInfo}");
        }


    }
}
