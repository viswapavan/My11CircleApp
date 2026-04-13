using Microsoft.AspNetCore.Mvc;
using My11CircleApp.Data;
using System.Linq;

namespace My11CircleApp.Controllers
{
    public class WalletController : Controller
    {
        private readonly AppDbContext _context;

        public WalletController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            var data = _context.WalletTransactions
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            return View(data);
        }
    }
}