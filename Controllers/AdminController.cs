using Microsoft.AspNetCore.Mvc;
using My11CircleApp.Data;
using My11CircleApp.Models;
using System;
using System.Linq;
using System.Collections.Generic;

namespace My11CircleApp.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ Admin Home Page
        public IActionResult Index()
        {
            ViewBag.Leagues = _context.Leagues.ToList();
            ViewBag.Matches = _context.Matches.ToList();
            ViewBag.Users = _context.Users.ToList(); // ✅ MUST EXIST

            return View();
        }
        [HttpPost]
        public IActionResult CreateUser(string name, string email, string password, string role)
        {
            try
            {
                var user = new User
                {
                    Name = name,
                    Email = email,
                    PasswordHash = Hash(password),
                    Role = role,
                    Wallet = 0 // ✅ important to avoid null issues
                };

                _context.Users.Add(user);
                _context.SaveChanges();

                TempData["Success"] = "User created successfully";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            // ✅ IMPORTANT FIX (DO NOT USE return View)
            return RedirectToAction("Index");
        }
        private string Hash(string input)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input)));
        }
        // ✅ Add League
        [HttpPost]
        public IActionResult AddLeague(string name, string description)
        {
            var league = new League
            {
                Name = name,
                Description = description
            };

            _context.Leagues.Add(league);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // ✅ Add Match
        public IActionResult AddMatch()
        {
            ViewBag.Leagues = _context.Leagues.ToList();
            ViewBag.Users = _context.Users.ToList();

            return View();
        }
        [HttpPost]
        public IActionResult AddMatch(int leagueId, DateTime matchDate, decimal entryFee, string description, List<int> selectedUsers)
        {
            var match = new Match
            {
                LeagueId = leagueId,
                MatchDate = matchDate,
                Status = "pending",
                EntryFee = entryFee,
                Description = description
            };

            _context.Matches.Add(match);
            _context.SaveChanges();

            // 💰 Deduct based on EntryFee
            if (selectedUsers != null)
            {
                foreach (var userId in selectedUsers)
                {
                    var user = _context.Users.Find(userId);

                    if (user != null && user.Wallet >= entryFee)
                    {
                        _context.MatchParticipants.Add(new MatchParticipant
                        {
                            MatchId = match.Id,
                            UserId = userId
                        });

                        user.Wallet -= entryFee;
                        _context.WalletTransactions.Add(new WalletTransaction
                        {
                            UserId = user.Id,
                            Amount = entryFee,
                            Type = "Debit",
                            Description = $"{match.Description} - Entry Fee"
                        });
                    }
                }
            }

            _context.SaveChanges();

            return RedirectToAction("Index");
        }
        // ✅ Load Result Entry Page
        public IActionResult AddResult()
        {
            ViewBag.Matches = _context.Matches
                .Where(m => m.Status == "pending")
                .ToList();

            ViewBag.Users = _context.Users.ToList();

            return View();
        }

        [HttpPost]
        public IActionResult AddMoney(int userId, decimal amount)
        {
            if (HttpContext.Session.GetString("Role") != "admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Users.Find(userId);

            if (user != null)
            {
                user.Wallet += amount;
                _context.WalletTransactions.Add(new WalletTransaction
                {
                    UserId = user.Id,
                    Amount = amount,
                    Type = "Credit",
                    Description = "Admin added money"
                });
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
        // ✅ Save Results (with prize split logic)
        [HttpPost]
        public IActionResult AddResult(int matchId, List<int> firstPlace, List<int> secondPlace, List<int> thirdPlace)
        {
            var match = _context.Matches.Find(matchId);
            if (match == null) return RedirectToAction("Index");

            var participants = _context.MatchParticipants
                .Where(p => p.MatchId == matchId)
                .ToList();

            int totalPlayers = participants.Count;
            if (totalPlayers == 0) return RedirectToAction("Index");

            decimal totalAmount = match.EntryFee * totalPlayers;

            // Prize pools
            decimal firstPool = 0;
            decimal secondPool = 0;
            decimal thirdPool = 0;

            // ---------------- 🥇 FIRST ----------------
            if (firstPlace != null && firstPlace.Count > 0)
            {
                if (firstPlace.Count == 1)
                    firstPool = totalAmount * 0.5m;

                else if (firstPlace.Count == 2)
                    firstPool = totalAmount * 0.8m; // 40% each

                else if (firstPlace.Count == 3)
                    firstPool = totalAmount * 1.0m; // full pool
            }

            // ---------------- 🥈 SECOND ----------------
            if (secondPlace != null && secondPlace.Count > 0)
            {
                if (secondPlace.Count == 1)
                    secondPool = totalAmount * 0.3m;

                else if (secondPlace.Count == 2)
                    secondPool = totalAmount * 0.5m; // 25% each

                else if (secondPlace.Count == 3)
                    secondPool = totalAmount * 0.5m;
            }

            // ---------------- 🥉 THIRD ----------------
            if (thirdPlace != null && thirdPlace.Count > 0)
            {
                // ❌ If 2nd has 2 winners → no 3rd prize
                if (secondPlace != null && secondPlace.Count == 2)
                {
                    thirdPool = 0;
                }
                else
                {
                    thirdPool = totalAmount * 0.2m;
                }
            }

            // ================= DISTRIBUTION =================

            // 🥇 First
            if (firstPlace != null && firstPlace.Count > 0 && firstPool > 0)
            {
                decimal split = firstPool / firstPlace.Count;

                foreach (var userId in firstPlace)
                {
                    AddResultEntry(matchId, userId, "1", split, $"{match.Description} - 1st Prize");
                }
            }

            // 🥈 Second
            if (secondPlace != null && secondPlace.Count > 0 && secondPool > 0)
            {
                decimal split = secondPool / secondPlace.Count;

                foreach (var userId in secondPlace)
                {
                    AddResultEntry(matchId, userId, "2", split, $"{match.Description} - 2nd Prize");
                }
            }

            // 🥉 Third
            if (thirdPlace != null && thirdPlace.Count > 0 && thirdPool > 0)
            {
                decimal split = thirdPool / thirdPlace.Count;

                foreach (var userId in thirdPlace)
                {
                    AddResultEntry(matchId, userId, "3", split, $"{match.Description} - 3rd Prize");
                }
            }

            match.Status = "completed";
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
        private void AddResultEntry(int matchId, int userId, string position, decimal amount, string description)
        {
            _context.ContestResults.Add(new ContestResult
            {
                MatchId = matchId,
                UserId = userId,
                Position = position,
                Prize = amount
            });

            var user = _context.Users.Find(userId);
            if (user != null)
            {
                user.Wallet += amount;

                _context.WalletTransactions.Add(new WalletTransaction
                {
                    UserId = user.Id,
                    Amount = amount,
                    Type = "Credit",
                    Description = description
                });
            }
        }
        public IActionResult EditResult(int matchId)
        {
            var match = _context.Matches.Find(matchId);

            var results = _context.ContestResults
                .Where(r => r.MatchId == matchId)
                .ToList();

            ViewBag.Match = match;
            ViewBag.Users = _context.Users.ToList();

            // 👇 group existing winners
            ViewBag.First = results.Where(r => r.Position == "1").Select(r => r.UserId).ToList();
            ViewBag.Second = results.Where(r => r.Position == "2").Select(r => r.UserId).ToList();
            ViewBag.Third = results.Where(r => r.Position == "3").Select(r => r.UserId).ToList();

            return View();
        }
        [HttpPost]
        public IActionResult EditResult(int matchId, List<int> firstPlace, List<int> secondPlace, List<int> thirdPlace)
        {
            // ✅ Step 1: Get old results
            var oldResults = _context.ContestResults
                .Where(r => r.MatchId == matchId)
                .ToList();

            // ✅ Step 2: REVERSE old wallet impact
            foreach (var r in oldResults)
            {
                var user = _context.Users.Find(r.UserId);
                if (user != null)
                {
                    user.Wallet -= r.Prize; // 🔥 remove old winnings
                }
            }

            // ✅ Remove old results
            _context.ContestResults.RemoveRange(oldResults);
            _context.SaveChanges();

            // ✅ Step 3: Calculate pool
            var participants = _context.MatchParticipants.Where(p => p.MatchId == matchId).ToList();
            var match = _context.Matches.Find(matchId);

            decimal totalPool = participants.Count * match.EntryFee;

            decimal firstPrize = totalPool * 0.5m;
            decimal secondPrize = totalPool * 0.3m;
            decimal thirdPrize = totalPool * 0.2m;

            // 🥇 First
            if (firstPlace != null && firstPlace.Count > 0)
            {
                decimal split = firstPrize / firstPlace.Count;

                foreach (var uid in firstPlace)
                {
                    _context.ContestResults.Add(new ContestResult
                    {
                        MatchId = matchId,
                        UserId = uid,
                        Position = "1",
                        Prize = split
                    });

                    var user = _context.Users.Find(uid);
                    user.Wallet += split;

                    _context.WalletTransactions.Add(new WalletTransaction
                    {
                        UserId = uid,
                        Amount = split,
                        Type = "Credit",
                        Description = "Updated 1st Prize"
                    });
                }
            }

            // 🥈 Second
            if (secondPlace != null && secondPlace.Count > 0)
            {
                decimal split = secondPrize / secondPlace.Count;

                foreach (var uid in secondPlace)
                {
                    _context.ContestResults.Add(new ContestResult
                    {
                        MatchId = matchId,
                        UserId = uid,
                        Position = "2",
                        Prize = split
                    });

                    var user = _context.Users.Find(uid);
                    user.Wallet += split;

                    _context.WalletTransactions.Add(new WalletTransaction
                    {
                        UserId = uid,
                        Amount = split,
                        Type = "Credit",
                        Description = "Updated 2nd Prize"
                    });
                }
            }

            // 🥉 Third
            if (thirdPlace != null && thirdPlace.Count > 0)
            {
                decimal split = thirdPrize / thirdPlace.Count;

                foreach (var uid in thirdPlace)
                {
                    _context.ContestResults.Add(new ContestResult
                    {
                        MatchId = matchId,
                        UserId = uid,
                        Position = "3",
                        Prize = split
                    });

                    var user = _context.Users.Find(uid);
                    user.Wallet += split;

                    _context.WalletTransactions.Add(new WalletTransaction
                    {
                        UserId = uid,
                        Amount = split,
                        Type = "Credit",
                        Description = "Updated 3rd Prize"
                    });
                }
            }

            _context.SaveChanges();

            return RedirectToAction("Index");
        }
        public IActionResult EditMatch(int matchId)
        {
            var match = _context.Matches.Find(matchId);
            if (match == null) return NotFound();

            return View(match);
        }
        [HttpPost]
        [HttpPost]
        public IActionResult EditMatch(int id, decimal entryFee, string description, DateTime matchDate)
        {
            var match = _context.Matches.Find(id);
            if (match == null) return NotFound();
            // 🔹 Update basic fields
            match.Description = description;
            match.MatchDate = matchDate;

            decimal oldFee = match.EntryFee;
            decimal diff = entryFee - oldFee;

            if (diff == 0)
            {
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            var participants = _context.MatchParticipants
                .Where(p => p.MatchId == id)
                .ToList();

            // ✅ 1. Adjust wallet (only difference)
            foreach (var p in participants)
            {
                var user = _context.Users.Find(p.UserId);
                if (user == null) continue;

                user.Wallet -= diff; // if diff = +1 → deduct extra ₹1

                _context.WalletTransactions.Add(new WalletTransaction
                {
                    UserId = user.Id,
                    Amount = Math.Abs(diff),
                    Type = diff > 0 ? "Debit" : "Credit",
                    Description = $"{match.Description} - Fee Correction"
                });
            }

            // ✅ 2. If results exist → fix winnings
            var oldResults = _context.ContestResults
                .Where(r => r.MatchId == id)
                .ToList();

            if (oldResults.Any())
            {
                // 🔁 reverse old winnings
                foreach (var r in oldResults)
                {
                    var user = _context.Users.Find(r.UserId);
                    if (user != null)
                        user.Wallet -= r.Prize;
                }

                _context.ContestResults.RemoveRange(oldResults);
                _context.SaveChanges();

                // 🔁 recalc with new entry fee
                decimal totalPool = participants.Count * entryFee;

                decimal firstPrize = totalPool * 0.5m;
                decimal secondPrize = totalPool * 0.3m;
                decimal thirdPrize = totalPool * 0.2m;

                // NOTE: you may want to store winners before delete
                // or force admin to re-enter via Edit Result UI
            }

            // ✅ 3. Update match fee
            match.EntryFee = entryFee;

            _context.SaveChanges();

            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult ResetPassword(int userId)
        {
            var user = _context.Users.Find(userId);
            if (user == null) return NotFound();

            // 🔐 set default password = 1234
            user.PasswordHash = Hash("1234");

            _context.SaveChanges();

            return RedirectToAction("Index");
        }   
    }
}