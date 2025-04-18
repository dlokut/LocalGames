using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Client.Database;

public class Playtime
{
    [Key]
    [ForeignKey(nameof(Game))]
    public Guid GameId { get; set; }
    
    public int PlaytimeMins { get; set; }
    
    public DownloadedGame Game { get; set; }
}