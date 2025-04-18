using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Client.Database;

public class ProtonSettings
{
    [Key]
    [ForeignKey(nameof(Game))]
    public Guid GameId { get; set; }
    
    public string ProtonVersion { get; set; }

    public string PrefixDir { get; set; }

    public bool MangohudEnabled { get; set; }
    
    public bool GamemodeEnabled { get; set; }

    public bool FSyncEnabled { get; set; }
    
    public bool ESyncEnabled { get; set; }

    public bool DxvkEnabled { get; set; }
    
    public bool DxvkAsync { get; set; }
    
    public bool DxvkFramerate { get; set; }

    public bool NvapiEnabled { get; set; }

    public DownloadedGame Game { get; set; }
}