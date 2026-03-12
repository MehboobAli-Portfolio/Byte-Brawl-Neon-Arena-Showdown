using Postgrest.Attributes;
using Postgrest.Models;

[Table("player_match_stats")]
public class PlayerMatchStats : BaseModel
{
    [PrimaryKey("statid", false)]
    public int StatID { get; set; }

    [Column("playerid")]
    public int PlayerID { get; set; }

    [Column("matchid")]
    public int MatchID { get; set; }

    [Column("kills")]
    public int Kills { get; set; }

    [Column("deaths")]
    public int Deaths { get; set; }

    [Column("score")]
    public int Score { get; set; }

}