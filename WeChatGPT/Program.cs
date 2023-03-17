using Microsoft.EntityFrameworkCore;
using OpenAI.GPT3.Extensions;
using WeChatGPT;
using WeChatGPT.Models;
using Pomelo.EntityFrameworkCore.MySql;
using WeChatGPT.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseKestrel(options =>
{
    options.AllowSynchronousIO = true;
});

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();

var Configuration = builder.Configuration;
AppSettings.OpenAiKey = Configuration["AppSettings:OpenAiKey"].ToString();
AppSettings.WebSite = Configuration["AppSettings:WebSite"].ToString();
AppSettings.WxOpenToken = Configuration["AppSettings:WxOpenToken"].ToString();
ApplicationDbContext.ConnectionString = Configuration.GetConnectionString("DefaultConnection");

// 连接MySQL数据库
var serverVersion = new MySqlServerVersion(new Version(5, 7));
builder.Services.AddDbContext<ApplicationDbContext>(
    dbContextOptions => dbContextOptions
            .UseMySql(ApplicationDbContext.ConnectionString, serverVersion)
        // The following three options help with debugging, but should
        // be changed or removed for production.
        .EnableSensitiveDataLogging()
        .EnableDetailedErrors()
    );
builder.Services  //注入OpenAI
    .AddOpenAIService(settings =>
    {
        settings.ApiKey = AppSettings.OpenAiKey;
    });
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
LogToFile.InitLogger(Configuration, app.Environment.WebRootPath);

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
