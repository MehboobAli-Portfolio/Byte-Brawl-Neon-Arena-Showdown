using System;
using Postgrest.Attributes;
using Postgrest.Models;

[Table("match_session")]
public class MatchSession : BaseModel
{
    [PrimaryKey("matchid", false)] // 'false' because the database auto-generates this ID
    public int MatchID { get; set; }

    [Column("mapname")]
    public string MapName { get; set; }

    [Column("gamemode")]
    public string GameMode { get; set; }

    [Column("starttime")]
    public DateTime StartTime { get; set; }

    [Column("endtime")]
    public DateTime EndTime { get; set; }
}