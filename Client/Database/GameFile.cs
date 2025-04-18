using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Client.Database;

[PrimaryKey(nameof(GameId), nameof(Directory))]
public class GameFile
{
    [ForeignKey(nameof(Game))]
    public Guid GameId { get; set; }
    
    public string Directory { get; set; }

    
    public bool IsMainExecutable { get; set; }
    
    public DownloadedGame Game { get; set; }
}