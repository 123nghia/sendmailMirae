using ToolCRM.Configuration;
using ToolCRM.Jobs;
using ToolCRM.Services;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure AppSettings
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

// Register services
builder.Services.AddScoped<SftpBrowserService>();

// Add Quartz services
builder.Services.AddQuartz(q =>
{
    
    // Register jobs
    var sendEmailJobKey = new JobKey("SendEmailJob");
    q.AddJob<SendEmailJob>(opts => opts.WithIdentity(sendEmailJobKey));
    
    var downloadFileJobKey = new JobKey("DownloadFileJob");
    q.AddJob<DownloadFileJob>(opts => opts.WithIdentity(downloadFileJobKey));
    
    var uploadFileJobKey = new JobKey("UploadFileReportJob");
    q.AddJob<UploadFileReportJob>(opts => opts.WithIdentity(uploadFileJobKey));
    
    // Get cron expressions from configuration
    var appSettings = builder.Configuration.GetSection("AppSettings").Get<AppSettings>();
    
    // Add triggers
    q.AddTrigger(opts => opts
        .ForJob(sendEmailJobKey)
        .WithIdentity("SendEmailTrigger")
        .WithCronSchedule(appSettings?.Quartz?.SendMailCron ?? "0 10,40 8-15 ? * MON,TUE,WED,THU,FRI,SAT *"));
    
    q.AddTrigger(opts => opts
        .ForJob(downloadFileJobKey)
        .WithIdentity("DownloadFileTrigger")
        .WithCronSchedule(appSettings?.Quartz?.DownloadFileCron ?? "0 0 8 ? * MON,TUE,WED,THU,FRI,SAT *"));
    
    q.AddTrigger(opts => opts
        .ForJob(uploadFileJobKey)
        .WithIdentity("UploadFileTrigger")
        .WithCronSchedule(appSettings?.Quartz?.UploadFileCron ?? "0 30 8 ? * MON,TUE,WED,THU,FRI,SAT *"));
});

// Add Quartz hosted service
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
