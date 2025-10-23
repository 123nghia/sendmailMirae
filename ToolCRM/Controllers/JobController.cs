using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Quartz;
using Quartz.Impl.Matchers;
using ToolCRM.Business;
using ToolCRM.Configuration;

namespace ToolCRM.Controllers
{
    public class JobController : Controller
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly AppSettings _appSettings;
        private readonly ILogger<JobController> _logger;

        public JobController(ISchedulerFactory schedulerFactory, IOptions<AppSettings> appSettings, ILogger<JobController> logger)
        {
            _schedulerFactory = schedulerFactory;
            _appSettings = appSettings.Value;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
            var triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());

            ViewBag.JobKeys = jobKeys;
            ViewBag.TriggerKeys = triggerKeys;
            ViewBag.SchedulerName = scheduler.SchedulerName;
            ViewBag.IsStarted = scheduler.IsStarted;

            return View();
        }

        public async Task<IActionResult> StartScheduler()
        {
            try
            {
                var scheduler = await _schedulerFactory.GetScheduler();
                if (!scheduler.IsStarted)
                {
                    await scheduler.Start();
                    _logger.LogInformation("Scheduler started successfully");
                    ViewBag.Message = "Scheduler started successfully";
                }
                else
                {
                    ViewBag.Message = "Scheduler is already running";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start scheduler");
                ViewBag.ErrorMessage = $"Failed to start scheduler: {ex.Message}";
            }

            return View("Index");
        }

        public async Task<IActionResult> StopScheduler()
        {
            try
            {
                var scheduler = await _schedulerFactory.GetScheduler();
                if (scheduler.IsStarted)
                {
                    await scheduler.Shutdown();
                    _logger.LogInformation("Scheduler stopped successfully");
                    ViewBag.Message = "Scheduler stopped successfully";
                }
                else
                {
                    ViewBag.Message = "Scheduler is already stopped";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop scheduler");
                ViewBag.ErrorMessage = $"Failed to stop scheduler: {ex.Message}";
            }

            return View("Index");
        }

        public async Task<IActionResult> TriggerSendEmailJob()
        {
            try
            {
                var scheduler = await _schedulerFactory.GetScheduler();
                var jobKey = new JobKey("SendEmailJob");
                
                if (await scheduler.CheckExists(jobKey))
                {
                    await scheduler.TriggerJob(jobKey);
                    _logger.LogInformation("SendEmailJob triggered manually");
                    ViewBag.Message = "SendEmailJob triggered successfully";
                }
                else
                {
                    ViewBag.ErrorMessage = "SendEmailJob not found";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to trigger SendEmailJob");
                ViewBag.ErrorMessage = $"Failed to trigger SendEmailJob: {ex.Message}";
            }

            return View("Index");
        }

        public async Task<IActionResult> TriggerDownloadFileJob()
        {
            try
            {
                var scheduler = await _schedulerFactory.GetScheduler();
                var jobKey = new JobKey("DownloadFileJob");
                
                if (await scheduler.CheckExists(jobKey))
                {
                    await scheduler.TriggerJob(jobKey);
                    _logger.LogInformation("DownloadFileJob triggered manually");
                    ViewBag.Message = "DownloadFileJob triggered successfully";
                }
                else
                {
                    ViewBag.ErrorMessage = "DownloadFileJob not found";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to trigger DownloadFileJob");
                ViewBag.ErrorMessage = $"Failed to trigger DownloadFileJob: {ex.Message}";
            }

            return View("Index");
        }
    }
}
