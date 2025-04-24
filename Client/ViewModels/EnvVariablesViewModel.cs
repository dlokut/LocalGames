using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Database;
using Client.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Client.ViewModels;

public partial class EnvVariablesViewModel : ViewModelBase
{
    private readonly ProtonSettings _protonSettings;
    private readonly string _gameName;

    [ObservableProperty] private List<ProtonEnvVariable> _envVariables;

    [ObservableProperty] private string _newVarKey;
    
    [ObservableProperty] private string _newVarValue;
    public EnvVariablesViewModel(ProtonSettings protonSettings, string gameName)
    {
        _protonSettings = protonSettings;
        _gameName = gameName;
        _ = PopulateEnvVars(protonSettings.GameId);
    }

    [RelayCommand]
    private async Task CreateNewEnvVar()
    {
        ProtonManager protonManager = new ProtonManager();
        await protonManager.AddProtonEnvVariableAsync(_protonSettings.GameId, NewVarKey, NewVarValue);

        await PopulateEnvVars(_protonSettings.GameId);
    }

    [RelayCommand]
    private void GoToSettings()
    {
        MainWindowViewModel.SwitchViews(new SettingsViewModel(_protonSettings, _gameName)
        {
            MainWindowViewModel = this.MainWindowViewModel
        });
    }

    private async Task PopulateEnvVars(Guid gameId)
    {
        ProtonManager protonManager = new ProtonManager();
        EnvVariables = await protonManager.GetAllEnvVariables(gameId);
    }
    
    
}