using Quartz;
using ToolCRM.Business;
using ToolCRM.Configuration;
using Microsoft.Extensions.Options;

namespace ToolCRM.Jobs
{
    public class UploadFileReportJob : IJob
    {
        private readonly ILogger<UploadFileReportJob> _logger;
        private readonly AppSettings _appSettings;

        public UploadFileReportJob(ILogger<UploadFileReportJob> logger, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _appSettings = appSettings.Value;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("UploadFileReportJob started at {Time}", DateTime.Now);
                
                var business = new HanldeBusiness(_appSettings);
                await business.UploadFilesToSFTP();
                
                _logger.LogInformation("UploadFileReportJob completed successfully at {Time}", DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UploadFileReportJob failed at {Time}", DateTime.Now);
            }
        }
    }
}
