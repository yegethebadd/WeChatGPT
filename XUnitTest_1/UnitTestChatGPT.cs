using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Xunit;
using OpenAI.GPT3.Extensions;
using OpenAI.GPT3.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Memory;
using System.Threading;
using WeChatGPT.Controllers;
using WeChatGPT.Models;
using WeChatGPT;
using WeChatGPT.Services;

namespace XUnitTest_1
{
    public class UnitTestChatGPT
    {
        private IWebHostEnvironment _environment;
        private IOptions<AppSettings> _appSettings;
        private ApplicationDbContext _context;
        private IConfiguration Configuration;
        private WeChatController _wechatController;
        private IOpenAIService _openaiService;
        private IMemoryCache _cache;

        public UnitTestChatGPT()
        {
            InitializeAppVariables();
        }

        [Fact]
        public async Task TestGetResponseFromChatGPT()
        {
            //var expired = DateTime.Now.AddMinutes(1);
            //_cache.Set("MsgId_" + 223, "好的的", expired);
            //if (_cache.TryGetValue("MsgId_" + 223, out var response))
            //{
            //    var content = response.ToString();
            //}

            //var text = await _laughterController.GetResponseFromChatGPT("公众号麻瓜分享，怎么样~");
            //Assert.True(!string.IsNullOrEmpty(text));
        }

        [Fact]
        public async Task TestHandleChatMsg()
        {
            var request = new RequestMessage
            {
                //Content = $"请问{new Random().Next(10)}*{new Random().Next(10, 20)}等于多少",
                Content = $"上一题的答案是多少",
                //FromUserName = "oulsRxBIsGrH7eKseXsRFLUI4DcY",
                FromUserName = "xxxxx",
                MsgId = 1328,
                MsgType = "text",
                ToUserName = "xxxxx"
            };
            var text = string.Empty;
            text = await _wechatController.HandleChatMsg(request);
            Assert.True(!string.IsNullOrEmpty(text));
            Thread.Sleep(2000);
        }

        private void InitializeAppVariables()
        {
            var services = new ServiceCollection();
            // 创建Mock IApplicationBuilder
            var mockApplicationBuilder = new Mock<IApplicationBuilder>();

            // 创建模拟IWebHostEnvironment对象
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            services.AddSingleton(mockEnvironment.Object);
            Configuration = new ConfigurationBuilder().AddJsonFile("appsettings.Development.json").Build();
            var appSettings = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettings);
            ApplicationDbContext.ConnectionString = Configuration.GetConnectionString("DefaultConnection");
            var serverVersion = new MySqlServerVersion(new Version(5, 7));
            services.AddDbContext<ApplicationDbContext>(
                dbContextOptions => dbContextOptions
                    .UseMySql(ApplicationDbContext.ConnectionString, serverVersion)
                // The following three options help with debugging, but should
                // be changed or removed for production.
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
            );
            AppSettings.OpenAiKey = Configuration["AppSettings:OpenAiKey"]?.ToString();
            services.AddOpenAIService(settings => { settings.ApiKey = AppSettings.OpenAiKey; });
            services.AddMemoryCache();
            LogToFile.InitLogger(Configuration, "D:\\Logs");

            var serviceProvider = services.BuildServiceProvider();
            _environment = serviceProvider.GetService<IWebHostEnvironment>();
            _appSettings = serviceProvider.GetRequiredService<IOptions<AppSettings>>();
            _context = serviceProvider.GetService<ApplicationDbContext>();
            //_context = .CreateDbContext();
            _openaiService = serviceProvider.GetRequiredService<IOpenAIService>();
            _cache = serviceProvider.GetRequiredService<IMemoryCache>();

            _wechatController = new WeChatController(_openaiService, _context, _cache);
        }
    }
}
