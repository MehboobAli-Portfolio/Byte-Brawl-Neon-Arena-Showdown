using Postgrest.Attributes;
using Postgrest.Models;

// Changed to strictly match PostgreSQL's lowercase format
[Table("player_account")]
public class PlayerAccount : BaseModel
{
    [Column("username")]
    public string Username { get; set; }

    [Column("email")]
    public string Email { get; set; }
}