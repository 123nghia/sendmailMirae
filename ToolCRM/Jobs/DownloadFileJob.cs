using Quartz;
using ToolCRM.Business;
using ToolCRM.Configuration;
using Microsoft.Extensions.Options;

namespace ToolCRM.Jobs
{
    public class DownloadFileJob : IJob
    {
        private readonly ILogger<DownloadFileJob> _logger;
        private readonly AppSettings _appSettings;

        public DownloadFileJob(ILogger<DownloadFileJob> logger, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _appSettings = appSettings.Value;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("DownloadFileJob started at {Time}", DateTime.Now);
                
                var business = new HanldeBusiness(_appSettings);
                await business.DownloadPaymentFile();
                
                _logger.LogInformation("DownloadFileJob completed successfully at {Time}", DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DownloadFileJob failed at {Time}", DateTime.Now);
            }
        }
    }
}
