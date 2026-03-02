using Postgrest.Attributes;
using Postgrest.Models;

// Changed to strictly lowercase to match PostgreSQL
[Table("player_account")]
public class PlayerAccount : BaseModel
{
    [Column("username")]
    public string Username { get; set; }

    [Column("email")]
    public string Email { get; set; }

    [Column("ranktierid")]
    public int RankTierID { get; set; }

    // --- NEW STATS ---
    [Column("totalkills")]
    public int TotalKills { get; set; }

    [Column("totaldeaths")]
    public int TotalDeaths { get; set; }

    [Column("playerlevel")]
    public int PlayerLevel { get; set; }
}