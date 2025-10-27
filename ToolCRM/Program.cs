var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure AppSettings
builder.Services.Configure<ToolCRM.Configuration.AppSettings>(
    builder.Configuration.GetSection("AppSettings"));

// Register services
builder.Services.AddScoped<ToolCRM.Business.Sendmail>();
builder.Services.AddScoped<ToolCRM.Business.HanldeBusiness>();

// Register background service
builder.Services.AddHostedService<ToolCRM.Services.PaymentAutoSendService>();

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
    pattern: "{controller=Sftp}/{action=Index}/{id?}");

app.Run();
