using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Client.Database;

namespace Client.Models;

public class ProtonManager
{
    private const string PROTON_VERSION_DIR = "Proton";
    
    private const string UMU_EXECUTABLE_PATH = "umu-run";

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

    public async Task LaunchGame()
    {
        string gamePath = Path.Combine(Directory.GetCurrentDirectory(), "Games/NNF_FULLVERSION.exe");
        string prefixPath = Path.Combine(Directory.GetCurrentDirectory(), "Games/pfx");
        string protonPath = Path.Combine(Directory.GetCurrentDirectory(), "Proton/GE-Proton9-27");
        
        Environment.SetEnvironmentVariable("WINEPREFIX", prefixPath);
        Environment.SetEnvironmentVariable("PROTONPATH", protonPath);

        Process process = Process.Start(UMU_EXECUTABLE_PATH, gamePath);
        
    }

    public async Task AddProtonEnvVariableAsync(Guid gameId, string key, string value)
    {
        ProtonEnvVariable protonEnvVariable = new ProtonEnvVariable()
        {
            GameId = gameId,
            Key = key,
            Value = value
        };

        using (ClientDbContext dbContext = new ClientDbContext())
        {
            await dbContext.ProtonEnvVariables.AddAsync(protonEnvVariable);
            await dbContext.SaveChangesAsync();
        }
    }
    
}