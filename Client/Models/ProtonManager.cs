using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Primitives;
using Client.Database;
using Microsoft.EntityFrameworkCore;

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

    public async Task<bool> CanLaunchGameAsync(Guid gameId)
    {
        using (ClientDbContext dbContext = new ClientDbContext())
        {
            GameFile? mainExecutible = await dbContext.GameFiles
                .FirstOrDefaultAsync(gf => (gf.GameId == gameId) && gf.IsMainExecutable);
            
            if (mainExecutible == null) return false;
                
            
            ProtonSettings settings = await dbContext.ProtonSettings.FindAsync(gameId);
            bool PROTON_VERSION_SELECTED = settings.ProtonVersion != null;
            if (!PROTON_VERSION_SELECTED) return false;

            return true;
        }
    }
    
    public async Task LaunchGame(Guid gameId)
    {
        ProtonSettings protonSettings;
        GameFile mainExecutable;

        using (ClientDbContext dbContext = new ClientDbContext())
        {
            protonSettings = await dbContext.ProtonSettings.FindAsync(gameId);
            
            mainExecutable = await dbContext.GameFiles
                .FirstOrDefaultAsync(gf => (gf.GameId == gameId) && gf.IsMainExecutable);
        }

        await SetProtonEnvVariablesAsync(gameId, protonSettings);
        string commandString = CreateWrapperCommandsString(protonSettings);

        commandString += UMU_EXECUTABLE_PATH;
        
        string absoluteMainExecutablePath =
            Path.Combine(Directory.GetCurrentDirectory(), GAMES_DIR, mainExecutable.Directory);
        commandString += " " + absoluteMainExecutablePath;

        var test = commandString.IndexOf(' ');
        string firstCommand = commandString.Substring(0, commandString.IndexOf(' '));
        string commandArguments = commandString.Substring(commandString.IndexOf(' ') + 1);

        Process process = Process.Start(firstCommand, commandArguments);
        
    }
    
    private async Task SetProtonEnvVariablesAsync(Guid gameId, ProtonSettings protonSettings)
    {
        List<ProtonEnvVariable> envVariables;
        using (ClientDbContext dbContext = new ClientDbContext())
        {
            envVariables = dbContext.ProtonEnvVariables.Where(e => e.GameId == gameId).ToList();
            protonSettings = await dbContext.ProtonSettings.FindAsync(gameId);
        }
        
        
        foreach (ProtonEnvVariable envVariable in envVariables)
        {
            Environment.SetEnvironmentVariable(envVariable.Key, envVariable.Value);
        }
        
        
        string absoluteProtonDir =
            Path.Combine(Directory.GetCurrentDirectory(), PROTON_VERSION_DIR, protonSettings.ProtonVersion);
        Environment.SetEnvironmentVariable("PROTONPATH", absoluteProtonDir);
        
        Environment.SetEnvironmentVariable("WINEPREFIX", protonSettings.PrefixDir);
        
        int fSyncDisabled = Convert.ToInt16(!protonSettings.FSyncEnabled);
        Environment.SetEnvironmentVariable("PROTON_NO_FSYNC", fSyncDisabled.ToString());
        
        int eSyncDisabled = Convert.ToInt16(!protonSettings.ESyncEnabled);
        Environment.SetEnvironmentVariable("PROTON_NO_ESYNC", eSyncDisabled.ToString());
        
        int dxvkEnabled = Convert.ToInt16(protonSettings.DxvkEnabled);
        Environment.SetEnvironmentVariable("PROTON_USE_DXVK", dxvkEnabled.ToString());
        
        int dxvkAsync = Convert.ToInt16(protonSettings.DxvkAsync);
        Environment.SetEnvironmentVariable("DXVK_ASYNC", dxvkAsync.ToString());
        
        int? UNLIMITED_FRAMERATE = null;
        if (protonSettings.DxvkFramerate != UNLIMITED_FRAMERATE)
        {
            Environment.SetEnvironmentVariable("DXVK_FRAME_RATE", protonSettings.DxvkFramerate.ToString());
        }
        
        int nvapiEnabled = Convert.ToInt16(protonSettings.NvapiEnabled);
        Environment.SetEnvironmentVariable("PROTON_ENABLE_NVAPI", nvapiEnabled.ToString());
        
    }
    
    private string CreateWrapperCommandsString(ProtonSettings settings)
    {
        List<string> wrapperCommands = new List<string>();
        
        if (settings.MangohudEnabled) wrapperCommands.Add("mangohud");
        if (settings.GamemodeEnabled) wrapperCommands.Add("gamemoderun");
        
        string joinedVariablesString = String.Join(" ", wrapperCommands);
        if (joinedVariablesString != "") joinedVariablesString += " ";
        
        return joinedVariablesString;

    }

    public async Task SetPrimaryExecutible(Guid gameId, string fileDir)
    {
        using (ClientDbContext dbContext = new ClientDbContext())
        {
            GameFile? gameFile = await dbContext.GameFiles.FindAsync(gameId, fileDir);

            if (gameFile == null)
            {
                throw new Exception("Selected file is not in game directory");
            }

            gameFile.IsMainExecutable = true;
            dbContext.GameFiles.Update(gameFile);

            await dbContext.SaveChangesAsync();
        }
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