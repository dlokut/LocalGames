using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Client.Database;

[PrimaryKey(nameof(GameId), nameof(Url))]
public class Artwork
{
    [ForeignKey(nameof(Game))]
    public Guid GameId { get; set; }
    
    public string Url { get; set; }
    
    public DownloadedGame Game { get; set; }
}