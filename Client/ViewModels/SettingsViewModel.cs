using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Client.Database;
using Client.Models;
using Client.Models.ServerApi;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Client.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ProtonSettings _protonSettings;
    
    private Dictionary<string, string> gameExeFilesByFileName;

    [ObservableProperty] private string _gameName;

    [ObservableProperty] private string _prefixDir;

    [ObservableProperty] private bool _eSyncEnabled;
    
    [ObservableProperty] private bool _fSyncEnabled;

    [ObservableProperty] private bool _dxvkEnabled;

    [ObservableProperty] private bool _dxvkFramerateSet;
    
    [ObservableProperty] private int? _dxvkFramerate;

    [ObservableProperty] private bool _mangohudEnabled;

    [ObservableProperty] private bool _gamemodeEnabled;

    [ObservableProperty] private bool _dxvkAsync;
    
    [ObservableProperty] private List<string> _exeFiles;

    [ObservableProperty] private List<string> _protonVersions;

    [ObservableProperty] private string? _chosenProtonVersion;

    [ObservableProperty] private string? _mainExeFile;
    
    public SettingsViewModel(ProtonSettings protonSettings, string gameName)
    {
        _protonSettings = protonSettings;

        GameName = gameName;
        PrefixDir = protonSettings.PrefixDir;
        ESyncEnabled = protonSettings.ESyncEnabled;
        FSyncEnabled = protonSettings.FSyncEnabled;
        DxvkEnabled = protonSettings.DxvkEnabled;
        DxvkAsync = protonSettings.DxvkAsync;
        DxvkFramerate = protonSettings.DxvkFramerate;
        DxvkFramerateSet = DxvkFramerate != null;
        MangohudEnabled = protonSettings.MangohudEnabled;
        GamemodeEnabled = protonSettings.GamemodeEnabled;

        SetProtonVersions();
        _ = SetExeFilesAsync();
        _ = SetMainExeFileAsync();
    }

    [RelayCommand]
    private async Task SaveSettings()
    {
        _protonSettings.PrefixDir = PrefixDir;
        _protonSettings.ESyncEnabled = ESyncEnabled;
        _protonSettings.FSyncEnabled = FSyncEnabled;
        _protonSettings.DxvkEnabled = DxvkEnabled;
        _protonSettings.DxvkAsync = DxvkAsync;
        _protonSettings.MangohudEnabled = MangohudEnabled;
        _protonSettings.GamemodeEnabled = GamemodeEnabled;
        _protonSettings.ProtonVersion = ChosenProtonVersion;

        if (DxvkFramerateSet) _protonSettings.DxvkFramerate = DxvkFramerate;
        else _protonSettings.DxvkFramerate = null;
        
        ProtonManager protonManager = new ProtonManager();
        await protonManager.SetProtonSettings(_protonSettings);

        if (MainExeFile != null)
        {
            string mainExePath = gameExeFilesByFileName[MainExeFile];
            await protonManager.SetPrimaryExecutible(_protonSettings.GameId, mainExePath);
        }
        
        GoToGameLibrary();
    }
    
    [RelayCommand]
    private void GoToGameLibrary()
    {
        MainWindowViewModel.SwitchViews(new GameLibraryViewModel()
        {
            MainWindowViewModel = this.MainWindowViewModel
        });
    }

    [RelayCommand]
    private async void GoToEnvVariables()
    {
        MainWindowViewModel.SwitchViews(new EnvVariablesViewModel(_protonSettings, GameName)
        {
            MainWindowViewModel = this.MainWindowViewModel
        });
    }

    private async Task SetExeFilesAsync()
    {
        GameApiClient gameApiClient = new GameApiClient();
        gameExeFilesByFileName = await gameApiClient.GetExeGameFilesAsync(_protonSettings.GameId);

        ExeFiles = gameExeFilesByFileName.Keys.ToList();
    }

    private async Task SetMainExeFileAsync()
    {
        GameApiClient gameApiClient = new GameApiClient();
        MainExeFile = await gameApiClient.GetMainExeFileName(_protonSettings.GameId);
    }

    private void SetProtonVersions()
    {
        ProtonManager protonManager = new ProtonManager();
        ProtonVersions = protonManager.GetProtonVersions();

        ChosenProtonVersion = _protonSettings.ProtonVersion;
    }
}