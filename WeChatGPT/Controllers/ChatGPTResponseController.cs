using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Text.RegularExpressions;
using WeChatGPT.Models;
using WeChatGPT;

namespace MaguaShare.Controllers
{
    public class ChatGPTResponseController : Controller
    {
        private ApplicationDbContext _context;
        public ChatGPTResponseController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string u, string m)
        {
            var one = _context.ChatgptRecords.FirstOrDefault(c => c.UniqueKey == u && c.MsgId == m);
            return View(one ?? new ChatgptRecord());
        }
    }
}
