using System.Text.RegularExpressions;

namespace WeChatGPT.Services
{
    /// <summary>
    /// 将日志记录到文档中
    /// 创建时间：2017-4-28
    /// </summary>
    public class LogToFile
    {
        public static string WebRootPath { get; set; }
        public static LogLevel LogLevel { get; set; } = LogLevel.Information;
        public static string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss"; // 时间格式，使用毫秒级记录
        public static void LogInformation(string message)
        {
            if (LogLevel <= LogLevel.Information)
                LogInformation(GetApplicationRoot(), message);
        }

        public static void LogHeartBeat(string message)
        {
            if (LogLevel <= LogLevel.HeartBeat)
                LogHeartBeat(GetApplicationRoot(), message);
        }

        public static void LogDebug(string message)
        {
            if (LogLevel <= LogLevel.Debug)
                LogDebug(GetApplicationRoot(), message);
        }

        public static void LogInformationSeperateStart()
        {
            if (LogLevel <= LogLevel.Information)
                LogInformation(GetApplicationRoot(), "---------------- Start ----------------");
        }

        public static void LogInformationSeperateEnd(string message = "")
        {
            if (LogLevel <= LogLevel.Information)
                LogInformation(GetApplicationRoot(), $"---------------- End.. ----------------{message}");
        }

        public static void LogWarning(string message)
        {
            if (LogLevel <= LogLevel.Warning)
                LogWarning(GetApplicationRoot(), message);
        }

        public static void LogError(string message)
        {
            if (LogLevel <= LogLevel.Error)
                LogError(GetApplicationRoot(), message);
        }

        public static void LogException(Exception exception)
        {
            if (LogLevel <= LogLevel.Warning)
            {
                var message = $@"【Message:{exception.Message}】
                【Source:{exception.Source}】
                【InnerMessage:{exception.InnerException?.Message}】
                【Trace:{exception.StackTrace}】";
                LogError(message);
            }
        }

        private static void LogDebug(string rootDirectory, string message)
        {
            //异步执行
            _ = Task.Run(() =>
            {
                string fileDirectory = Path.Combine(rootDirectory, "Logs");
                Log(fileDirectory, "[Debug]", message);
            });
        }

        private static void LogInformation(string rootDirectory, string message)
        {
            //异步执行
            _ = Task.Run(() =>
            {
                string fileDirectory = Path.Combine(rootDirectory, "Logs");
                Log(fileDirectory, "[Info]", message);
            });
        }

        private static void LogHeartBeat(string rootDirectory, string message)
        {
            //异步执行
            _ = Task.Run(() =>
            {
                string fileDirectory = Path.Combine(rootDirectory, "Logs", "HeartBeat");
                Log(fileDirectory, "[Beat]", message);
            });
        }

        private static void LogWarning(string rootDirectory, string message)
        {
            //异步执行
            _ = Task.Run(() =>
            {
                string fileDirectory = Path.Combine(rootDirectory, "Logs");
                Log(fileDirectory, "[Info]", message);
            });
        }

        private static void LogError(string rootDirectory, string message)
        {
            //异步执行
            _ = Task.Run(() =>
            {
                string fileDirectory = Path.Combine(rootDirectory, "Logs", "Errors");
                Log(fileDirectory, "[ERR]", message);
            });
        }

        private static readonly object lockObj = new object(); // 线程锁对象，保证日志记录的原子性
        private static void Log(string fileDirectory, string stampType, string message)
        {
            try
            {
                lock (lockObj) // 加锁，避免多个线程同时写入文件，导致文件损坏
                {
                    string path = Path.Combine(fileDirectory, DateTime.Now.ToUniversalTime().AddHours(8).ToString("yyyyMMdd") + ".txt");
                    using (var fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read)) // 打开或创建文件流
                    using (var streamWriter = new StreamWriter(fileStream)) // 创建写入器
                    {
                        streamWriter.BaseStream.Seek(0, SeekOrigin.End); // 移动到文件结尾，以便追加日志信息
                        streamWriter.WriteLine(DateTime.Now.ToUniversalTime().AddHours(8).ToString(DateFormat) + $" {stampType}" + message); // 写入日志信息
                    }
                }
            }
            catch (Exception e)
            {
                if (e is DirectoryNotFoundException)
                {
                    try
                    {
                        Directory.CreateDirectory(fileDirectory);
                        Log(fileDirectory, stampType, message);
                    }
                    catch { }
                }
            }
        }

        public static void InitLogger(IConfiguration configuration, string rootPath = null)
        {
            var configPath = configuration["LogToFile:WebRootPath"]?.ToString();
            var dataFormat = configuration["LogToFile:DateFormat"]?.ToString();
            var logLevel = configuration["LogToFile:LogLevel"]?.ToString();
            WebRootPath = string.IsNullOrEmpty(configPath) ? rootPath : configPath;
            if (!string.IsNullOrEmpty(logLevel) && Enum.TryParse(logLevel, out LogLevel level))
            {
                LogLevel = level;
            }
            if (!string.IsNullOrEmpty(dataFormat))
            {
                DateFormat = dataFormat;
            }
        }

        public static string GetApplicationRoot()
        {
            if (string.IsNullOrEmpty(WebRootPath))
            {
                var exePath = Path.GetDirectoryName(System.Reflection
                                  .Assembly.GetExecutingAssembly().Location);
                Regex appPathMatcher = new Regex(@"(?<!fil)[A-Za-z]:\\+[\S\s]*?(?=\\+bin)");
                var appRoot = appPathMatcher.Match(exePath).Value;
                WebRootPath = Path.Combine(appRoot, "wwwroot"); //使用web时，通常需要此作为根目录
            }
            return WebRootPath;
        }
    }

    public enum LogLevel
    {
        Trace,
        Debug,
        HeartBeat,
        Information,
        Warning,
        Error,
        Critical,
        None
    }
}
