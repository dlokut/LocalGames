using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Client.Models;

public class ProtonManager
{
    private const string PROTON_VERSION_DIR = "Proton";

    public List<string> GetProtonVersions()
    {
        if (!Directory.Exists(PROTON_VERSION_DIR))
        {
            Directory.CreateDirectory(PROTON_VERSION_DIR);
        }
        
        List<string> protonVersions = Directory.GetDirectories(PROTON_VERSION_DIR).Select(d => 
            d.Substring(d.IndexOf(Path.DirectorySeparatorChar) + 1)).ToList();
        
        return protonVersions;
    }
}