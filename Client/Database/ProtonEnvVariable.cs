using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Client.Database;

[PrimaryKey(nameof(GameId), nameof(Key))]
public class ProtonEnvVariable
{
    [ForeignKey(nameof(Game))]
    public Guid GameId { get; set; }
    
    public string Key { get; set; }
    
    public string Value { get; set; }
    
    public DownloadedGame Game { get; set; }
}