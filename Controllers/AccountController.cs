using Microsoft.AspNetCore.Mvc;
using My11CircleApp.Data;
using My11CircleApp.Models;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace My11CircleApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // 🔐 Disable Register (only admin creates users)
        public IActionResult Register()
        {
            return RedirectToAction("Login");
        }

        // ✅ Login Page
        public IActionResult Login()
        {
            return View();
        }

        // ✅ Login POST
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null || user.PasswordHash != Hash(password))
            {
                ViewBag.Error = "Invalid credentials";
                return View();
            }

            // Store session
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Role", user.Role);
            HttpContext.Session.SetString("UserName", user.Name);
            HttpContext.Session.SetString("Wallet", user.Wallet.ToString());
            return RedirectToAction("Index", "Dashboard");

        }

        // 🔓 Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // 🔐 Password Hashing
        private string Hash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }
        // TEMP API
public IActionResult CreateAdmin()
{
    var user = new User
    {
        Name = "Admin",
        Email = "admin@gmail.com",
        PasswordHash = Hash("1234"),
        Role = "admin",
        Wallet = 1000
    };

    _context.Users.Add(user);
    _context.SaveChanges();

    return Content("Admin created");
}
    }
}
