namespace WeChatGPT.Models
{
    public class ChatgptRecord
    {
        public int ID { get; set; }

        /// <summary>
        /// 日期，格式yyyyMMdd
        /// </summary>
        public int TheDay { get; set; }

        /// <summary>
        /// 微信请求的MsgId
        /// </summary>
        public string MsgId { get; set; }

        /// <summary>
        /// 公众号用户ID
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// 提问问题
        /// </summary>
        public string Question { get; set; }

        /// <summary>
        /// chatgpt回复
        /// </summary>
        public string Answer { get; set; }

        /// <summary>
        /// 本地生成的
        /// </summary>
        public string UniqueKey { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.Now;
        public DateTime UpdateTime { get; set; } = DateTime.Now;
    }
}
