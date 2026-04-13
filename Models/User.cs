using System;

namespace My11CircleApp.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";   // ✅ Fix warning
        public string Email { get; set; } = "";  // ✅ Fix warning
        public string PasswordHash { get; set; } = ""; // ✅ Fix warning

        public string Role { get; set; } = "player";

        // ✅ IMPORTANT (THIS WAS MISSING)
        public decimal Wallet { get; set; } = 0;
    }
}