using System;

namespace My11CircleApp.Models
{
    public class Match
    {
        public int Id { get; set; }
        public int LeagueId { get; set; }
        public DateTime MatchDate { get; set; }
        public string Status { get; set; } = "pending";
        public decimal EntryFee { get; set; }
        public string Description { get; set; } = "";
    }
}