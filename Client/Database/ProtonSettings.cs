using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Client.Database;

public class ProtonSettings
{
    [Key]
    [ForeignKey(nameof(Game))]
    public Guid GameId { get; set; }

    public string? ProtonVersion { get; set; } = null;

    public string PrefixDir { get; set; }

    public bool MangohudEnabled { get; set; } = false;

    public bool GamemodeEnabled { get; set; } = false;

    public bool FSyncEnabled { get; set; } = true;

    public bool ESyncEnabled { get; set; } = true;

    public bool DxvkEnabled { get; set; } = true;

    public bool DxvkAsync { get; set; } = false;

    public int? DxvkFramerate { get; set; } = null;

    public bool NvapiEnabled { get; set; } = true;

    public DownloadedGame Game { get; set; }
}