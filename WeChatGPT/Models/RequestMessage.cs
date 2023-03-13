namespace WeChatGPT.Models
{
    public class RequestMessage
    {
        public string ToUserName { get; set; }
        public string FromUserName { get; set; }
        public string CreateTime { get; set; }
        public string MsgType { get; set; }
        public string Content { get; set; }
        public long MsgId { get; set; }
    }
}
