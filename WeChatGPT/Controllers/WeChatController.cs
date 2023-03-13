using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.Tokenizer.GPT3;
using System.Text;
using System.Xml.Linq;
using WeChatGPT.Models;
using WeChatGPT.Services;

namespace WeChatGPT.Controllers
{
    [Route("api/wechat")]
    public class WeChatController : Controller
    {
        private IOpenAIService _openAIService;
        private static IMemoryCache _cache;
        private ApplicationDbContext _context;

        public WeChatController(IOpenAIService openAIService,
            ApplicationDbContext context,
            IMemoryCache cache)
        {
            _openAIService = openAIService;
            _cache = cache;
            _context = context;
        }
        /// <summary>
        /// 获取来自ChatGPT的回应
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private async Task<string> GetResponseFromChatGPT(List<ChatMessage> msg)
        {
            var result = string.Empty;
            try
            {
                // 统计问题的token长度
                var by = TokenizerGpt3.Encode(msg.Last().Content);
                var completionResult = await _openAIService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
                {
                    Messages = msg,
                    Model = OpenAI.GPT3.ObjectModels.Models.ChatGpt3_5Turbo,
                    MaxTokens = 2048 - by.Count//optional
                });
                if (completionResult.Successful)
                {
                    result = completionResult.Choices.First().Message.Content;
                }
            }
            catch (Exception ex)
            {
                result += "ChatGPT获取出现失败，请联系管理员";
                LogToFile.LogException(ex);
            }

            return result.TrimStart('？').TrimStart('\n');
        }

        [HttpGet]
        [ActionName("Index")]
        public IActionResult Get(string signature, string timestamp, string nonce, string echostr)
        {
            if (!CheckSignature(signature, timestamp, nonce))
            {
                return Content("消息并非来自微信");
                //接收消息
            }

            return Content(echostr);
        }

        [HttpPost]
        [ActionName("Index")]
        public async Task<IActionResult> Post(string signature, string timestamp, string nonce, string echostr)
        {
            if (!CheckSignature(signature, timestamp, nonce))
            {
                return Content("消息并非来自微信");
            }

            LogToFile.LogInformation("收到微信请求");
            var doc = XDocument.Load(Request.Body);
            RequestMessage requestMessage = new RequestMessage();
            FillEntityWithXml(requestMessage, doc);
            switch (requestMessage.MsgType)
            {
                case "text":
                    return Content(await HandleChatMsg(requestMessage));
                default:
                    break;
            }
            return Content(echostr);
        }

        /// <summary>
        /// 验证是否微信发送消息
        /// </summary>
        /// <returns></returns>
        private bool CheckSignature(string signature, string timestamp, string nonce)
        {
            var arr = new[] { AppSettings.WxOpenToken, timestamp, nonce }.OrderBy(z => z).ToArray();
            var arrString = string.Join("", arr);
            var sha1 = System.Security.Cryptography.SHA1.Create();
            var sha1Arr = sha1.ComputeHash(Encoding.UTF8.GetBytes(arrString));
            var enText = new StringBuilder();
            foreach (var b in sha1Arr)
            {
                enText.AppendFormat("{0:x2}", b);
            }
            if (signature == enText.ToString())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void FillEntityWithXml<T>(T entity, XDocument doc) where T : RequestMessage, new()
        {
            var root = doc.Root;
            var props = entity.GetType().GetProperties();
            foreach (var prop in props)
            {
                var propName = prop.Name;
                if (root.Element(propName) != null)
                {
                    switch (prop.PropertyType.Name)
                    {
                        case "DateTime":
                            prop.SetValue(entity, new DateTime(long.Parse(root.Element(propName).Value)), null);
                            break;
                        case "Boolean":
                            if (propName == "FuncFlag")
                            {
                                prop.SetValue(entity, root.Element(propName).Value == "1", null);
                            }
                            else
                            {
                                goto default;
                            }
                            break;
                        case "Int64":
                            prop.SetValue(entity, long.Parse(root.Element(propName).Value), null);
                            break;
                        case "RequestMsgType":
                        default:
                            prop.SetValue(entity, root.Element(propName).Value, null);
                            break;
                    }
                }
            }
        }

        private static XDocument ConvertEntityToXml<T>(T entity) where T : class, new()
        {
            entity = entity ?? new T();
            var doc = new XDocument();
            doc.Add(new XElement("xml"));
            var root = doc.Root;

            //微信对字段排序有严格要求，这里对排序进行强制约束
            var propNameOrder = new[] { "ToUserName", "FromUserName", "CreateTime", "MsgType", "Content", "ArticleCount", "FuncFlag",/*以下是Atricle属性*/"Title ", "Description ", "PicUrl", "Url" }.ToList();
            Func<string, int> orderByPropName = propNameOrder.IndexOf;

            var props = entity.GetType().GetProperties().OrderBy(p => orderByPropName(p.Name)).ToList();
            foreach (var prop in props)
            {
                var propName = prop.Name;
                if (propName == "Aritcles")
                {
                    var articlesElement = new XElement("Articles");
                }
                else
                {
                    switch (prop.PropertyType.Name)
                    {
                        case "String":
                            root.Add(new XElement(propName,
                                new XCData(prop.GetValue(entity, null) as string ?? "")));
                            break;
                        case "DateTime":
                            root.Add(new XElement(propName, ((DateTime)prop.GetValue(entity, null)).Ticks));
                            break;
                        case "Boolean":
                            if (propName == "FuncFlag")
                            {
                                root.Add(new XElement(propName, (bool)prop.GetValue(entity, null) ? "1" : "0"));
                            }
                            else
                            {
                                goto default;
                            }
                            break;
                        case "Article":
                            root.Add(new XElement(propName, prop.GetValue(entity, null).ToString().ToLower()));
                            break;
                        default:
                            root.Add(new XElement(propName, prop.GetValue(entity, null)));
                            break;
                    }
                }
            }
            return doc;
        }

        public async Task<string> HandleChatMsg(RequestMessage requestMessage)
        {
            var contentResult = string.Empty;
            if (!string.IsNullOrEmpty(requestMessage.Content))
            {
                var expired = DateTime.Now.AddMinutes(5);
                var msgId = requestMessage.MsgId;
                if (_cache.TryGetValue<int>("MarkMsgId_" + msgId, out var v))
                {
                    v += 1;
                    _cache.Set("MarkMsgId_" + msgId, v, expired);
                    Thread.Sleep(3500);
                }
                else
                {
                    v = 1; // 第一次
                    _cache.Set("MarkMsgId_" + msgId, v, expired);
                    _cache.Set("Uniquekey_" + msgId, UniqueKey.GetUniqueKey(32), expired);
                }
                var content = string.Empty;
                var responseMessage = new ResponseMessage
                {
                    ToUserName = requestMessage.FromUserName,
                    FromUserName = requestMessage.ToUserName,
                    CreateTime = requestMessage.CreateTime,
                    MsgType = "text"
                };

                if (_cache.TryGetValue("MsgId_" + msgId, out var response))
                {
                    LogToFile.LogInformation($"MsgId_{msgId}缓存获取成功.");
                    content = response.ToString();
                }
                else if (v == 1) // 只获取一次
                {
                    int theDay = _cache.GetOrCreate("TheDay", (cache) =>
                    {
                        var todayLastTime = DateTime.Now.Date.AddDays(1).AddSeconds(-1);
                        cache.AbsoluteExpiration = todayLastTime;
                        return int.Parse(DateTime.Now.ToString("yyyyMMdd"));
                    });
                    LogToFile.LogDebug("从数据库获取当天会话");
                    // 将单用户当天的后面15条会话上下文关联起
                    var records = _context.ChatgptRecords
                        .Where(c => c.TheDay == theDay && c.UserId == requestMessage.FromUserName)
                        .OrderByDescending(c => c.ID).Take(15)
                        .OrderBy(c => c.ID)
                        .Select(c => c.Question)
                        .ToList();
                    LogToFile.LogDebug("从数据库获取完毕");
                    var msgL = new List<ChatMessage>();
                    foreach (var record in records)
                    {
                        msgL.Add(ChatMessage.FromUser(record));
                    }
                    msgL.Add(ChatMessage.FromUser(requestMessage.Content));
                    LogToFile.LogDebug("开始请求ChatGPT");
                    content = await GetResponseFromChatGPT(msgL);
                    LogToFile.LogDebug("请求ChatGPT完毕");
                    _ = SaveResponseAsync(requestMessage, content, theDay); // 异步处理结果
                }
                else if (v == 2) // 第二次未获取到的时候，啥也不干，不回复，等接口超时（微信5s）
                {
                    Thread.Sleep(2000);
                }
                else if (v == 3) // 第三次还未产生结果，返回UniqueKey链接，以存储结果
                {
                    var uniqueKey = _cache.Get<string>("Uniquekey_" + msgId);
                    var msg = "抱歉，未能在15s内响应，稍侯可点击链接查看ChatGPT回复。\n";
                    msg += $"{AppSettings.WebSite}/ChatGPTResponse?u={uniqueKey}&m={msgId}";
                    content = msg;
                }
                responseMessage.Content = content;

                var responseDoc = ConvertEntityToXml(responseMessage);

                contentResult = responseDoc.ToString();
                LogToFile.LogDebug("开始返回信息给用户");
            }
            return contentResult;
        }

        private static async Task SaveResponseAsync(RequestMessage requestMessage, string content, int theDay)
        {
            var expired = DateTime.Now.AddMinutes(5);
            LogToFile.LogInformation($"将ChatGPT回复发送给用户{requestMessage.FromUserName}");
            var msgId = requestMessage.MsgId;
            var uniqueKey = _cache.Get<string>("Uniquekey_" + msgId);

            var chatRecord = new ChatgptRecord
            {
                Answer = content,
                MsgId = msgId.ToString(),
                Question = requestMessage.Content,
                TheDay = theDay,
                UserId = requestMessage.FromUserName,
                UniqueKey = uniqueKey
            };
            using var context = new ApplicationDbContext();
            context.ChatgptRecords.Add(chatRecord);
            await context.SaveChangesAsync();

            _cache.Set("MsgId_" + msgId, content, expired);
            LogToFile.LogInformation($"MsgId_{msgId}缓存创建成功，内容如下：\n{content}");
        }
    }
}
