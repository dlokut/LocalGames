using System;
using System.Collections.Generic;

namespace Client.Database;

public class DownloadedGame
{
    public Guid Id { get; set; }
    
    public long FileSize { get; set; }
    
    public string Name { get; set; }
    
    public string Summary { get; set; }
    
    public string CoverUrl { get; set; }
    
    public List<ProtonEnvVariable> ProtonEnvVariables { get; set; }
    
    public List<Artwork> Artworks { get; set; }
    
    
}