namespace WeChatGPT.Models
{
    public class AppSettings
    {
        /// <summary>
        /// 网站地址
        /// </summary>
        public static string WebSite { get; set; }

        /// <summary>
        /// 微信开放平台token
        /// </summary>
        public static string WxOpenToken { get; set; }

        /// <summary>
        /// OpenAI的key
        /// </summary>
        public static string OpenAiKey { get; set; }

    }
}
