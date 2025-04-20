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

    private const string GAMES_DIR = "Games";

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

    public ProtonSettings CreateDefaultProtonSettings(DownloadedGame game)
    {
        string gameFolderName = game.Name;
        string absolutePrefixDir = Path.Combine(Directory.GetCurrentDirectory(), GAMES_DIR, gameFolderName, "pfx");
        string? firstFoundProton = GetProtonVersions().FirstOrDefault();

        ProtonSettings defaultSettings = new ProtonSettings()
        {
            GameId = game.Id,
            ProtonVersion = firstFoundProton,
            PrefixDir = absolutePrefixDir
        };

        return defaultSettings;
    }

    private async Task<string> CreateEnvVariableStringAsync(Guid gameId)
    {
        List<ProtonEnvVariable> envVariables;
        using (ClientDbContext dbContext = new ClientDbContext())
        {
            envVariables = dbContext.ProtonEnvVariables.Where(e => e.GameId == gameId).ToList();
        }

        if (envVariables.Count == 0)
        {
            return "";
        }

        List<string> envVariablesStrings = envVariables.Select(e => $"{e.Key}={e.Value}").ToList();
        string joinedVariablesString = string.Join(" ", envVariablesStrings);

        return joinedVariablesString;

    }

    public string CreateProtonSettingsEnvVariableString(ProtonSettings settings)
    {
        List<string> envVariablesStrings = new List<string>();

        if (settings.ProtonVersion != null)
        {
            string absoluteProtonDir =
                Path.Combine(Directory.GetCurrentDirectory(), PROTON_VERSION_DIR, settings.ProtonVersion);
            envVariablesStrings.Add($"PROTONPATH={absoluteProtonDir}");
        }
        
        envVariablesStrings.Add($"WINEPREFIX={settings.PrefixDir}");

        int fSyncDisabled = Convert.ToInt16(!settings.FSyncEnabled);
        envVariablesStrings.Add($"PROTON_NO_FSYNC={fSyncDisabled}");
        
        int eSyncDisabled = Convert.ToInt16(!settings.ESyncEnabled);
        envVariablesStrings.Add($"PROTON_NO_ESYNC={eSyncDisabled}");

        int dxvkEnabled = Convert.ToInt16(settings.DxvkEnabled);
        envVariablesStrings.Add($"PROTON_USE_DXVK={dxvkEnabled}");

        int dxvkAsync = Convert.ToInt16(settings.DxvkAsync);
        envVariablesStrings.Add($"DXVK_ASYNC={dxvkAsync}");

        int? UNLIMITED_FRAMERATE = null;
        if (settings.DxvkFramerate != UNLIMITED_FRAMERATE)
        {
            envVariablesStrings.Add($"DXVK_FRAME_RATE={settings.DxvkFramerate}");
        }

        int nvapiEnabled = Convert.ToInt16(settings.NvapiEnabled);
        envVariablesStrings.Add($"PROTON_ENABLE_NVAPI={nvapiEnabled}");

        string joinedVariablesString = String.Join(" ", envVariablesStrings);
        return joinedVariablesString;

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

    public async Task RemoveProtonEnvVariableAsync(Guid gameId, string key)
    {
        using (ClientDbContext dbContext = new ClientDbContext())
        {
            ProtonEnvVariable envVariableToRemove = await dbContext.ProtonEnvVariables.
                FindAsync(gameId, key);

            dbContext.Remove(envVariableToRemove);
            await dbContext.SaveChangesAsync();
        }
    }
    
}