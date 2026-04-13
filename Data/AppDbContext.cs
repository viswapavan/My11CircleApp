
using Microsoft.EntityFrameworkCore;
using My11CircleApp.Models;

namespace My11CircleApp.Data {
public class AppDbContext : DbContext {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<User> Users { get; set; }
    public DbSet<League> Leagues { get; set; }
    public DbSet<Match> Matches { get; set; }
    public DbSet<ContestResult> ContestResults { get; set; }
    public DbSet<MatchParticipant> MatchParticipants { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
    }
}
