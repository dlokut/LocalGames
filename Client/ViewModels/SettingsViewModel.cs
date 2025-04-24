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

    [ObservableProperty] private List<string> _exeFiles;

    [ObservableProperty] private List<string> _protonVersions;

    [ObservableProperty] private string? _chosenProtonVersion;

    [ObservableProperty] private string? _mainExeFile;
    
    public SettingsViewModel(ProtonSettings protonSettings)
    {
        _protonSettings = protonSettings;

        SetProtonVersions();
        _ = SetExeFilesAsync();
        _ = SetMainExeFileAsync();
    }

    [RelayCommand]
    private void GoToGameLibrary()
    {
        MainWindowViewModel.SwitchViews(new GameLibraryViewModel()
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