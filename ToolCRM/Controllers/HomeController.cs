using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using ToolCRM.Business;
using ToolCRM.Models;

namespace ToolCRM.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HanldeBusiness bussines;

        public HomeController(ILogger<HomeController> logger, HanldeBusiness business)
        {
            _logger = logger;
            bussines = business;
        }

        public async Task<IActionResult> Index(InputRequest? request)
        {
            if (request == null || request.DayReport.HasValue == false)
            {
                return View();
            }
            if (request.DayReport.Value.Date >= DateTime.Today)
            {
                ModelState.AddModelError(nameof(InputRequest.DayReport), "Ngày làm báo cáo chỉ được chọn trong quá khứ và không vượt quá ngày hôm qua.");
                return View(request);
            }
            var fileCV = request.FileTC;
            var fileReport = request.FileReport;
            var dayReport = request.DayReport;
            var result = await bussines.MoveFileInputFormAsync(request);

            if(string.IsNullOrEmpty(result))
            {
                return View("Success");
            }
            ViewBag.ErrorMesage = result;
            return View("IndexError");
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
    }
}
