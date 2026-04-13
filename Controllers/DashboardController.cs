
using Microsoft.AspNetCore.Mvc;
using My11CircleApp.Data;
using System.Linq;

namespace My11CircleApp.Controllers {
public class DashboardController : Controller {
    private readonly AppDbContext _context;
    public DashboardController(AppDbContext context) { _context = context; }

        public IActionResult Index()
        {
            var users = _context.Users.ToList();
            var matches = _context.Matches.Count();

            var result = users.Select(u =>
            {
                var played = _context.MatchParticipants.Count(p => p.UserId == u.Id);

                var winnings = _context.ContestResults
                    .Where(r => r.UserId == u.Id)
                    .Sum(r => (decimal?)r.Prize) ?? 0;

                var totalSpent = played * 10;
                var loss = totalSpent - winnings;

                return new
                {
                    Name = u.Name,
                    Played = played,
                    Missed = matches - played,
                    Profit = winnings,
                    Loss = loss
                };
            }).ToList();

            return View(result);
        }
    }
}
