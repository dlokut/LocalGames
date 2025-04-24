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
    private readonly Guid _gameId;
    
    [ObservableProperty] private List<ProtonEnvVariable> _envVariables;

    [ObservableProperty] private string _newVarKey;
    
    [ObservableProperty] private string _newVarValue;
    public EnvVariablesViewModel(Guid gameId)
    {
        _gameId = gameId;
        _ = PopulateEnvVars(gameId);
    }

    [RelayCommand]
    private async Task CreateNewEnvVar()
    {
        ProtonManager protonManager = new ProtonManager();
        await protonManager.AddProtonEnvVariableAsync(_gameId, NewVarKey, NewVarValue);
    }

    private async Task PopulateEnvVars(Guid gameId)
    {
        ProtonManager protonManager = new ProtonManager();
        EnvVariables = await protonManager.GetAllEnvVariables(gameId);
    }
    
    
}