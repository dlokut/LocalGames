using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Models.ServerApi;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Client.ViewModels;

public partial class GameLibraryViewModel : ViewModelBase
{
    [ObservableProperty] private ViewModelBase _splitViewContentViewModel;

    [ObservableProperty] private List<ServerGame> _uninstalledGames;

    public GameLibraryViewModel()
    {
    }

    [RelayCommand]
    private async Task Test()
    {
        await PopulateUninstalledGames();
    }
    
    private async Task PopulateUninstalledGames()
    {
        GameApiClient gameApiClient = new GameApiClient();
        UninstalledGames = await gameApiClient.GetAllGamesOnServer();
    }
}