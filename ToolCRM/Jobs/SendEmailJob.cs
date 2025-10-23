using Quartz;
using ToolCRM.Business;
using ToolCRM.Configuration;
using Microsoft.Extensions.Options;

namespace ToolCRM.Jobs
{
    public class SendEmailJob : IJob
    {
        private readonly ILogger<SendEmailJob> _logger;
        private readonly AppSettings _appSettings;

        public SendEmailJob(ILogger<SendEmailJob> logger, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _appSettings = appSettings.Value;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("SendEmailJob started at {Time}", DateTime.Now);
                
                var business = new HanldeBusiness(_appSettings);
                await business.SendEmailReport();
                
                _logger.LogInformation("SendEmailJob completed successfully at {Time}", DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendEmailJob failed at {Time}", DateTime.Now);
            }
        }
    }
}
